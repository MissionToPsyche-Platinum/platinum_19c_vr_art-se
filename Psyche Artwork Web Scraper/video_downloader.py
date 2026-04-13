"""
video_downloader.py
-------------------
Handles iframe-embedded video downloads from YouTube and Vimeo.

Pipeline for YouTube:
  1. Download highest-quality video-only stream  (yt-dlp, format: bv/b)
  2. Download highest-quality audio-only stream  (yt-dlp, format: ba/b)
  3. Convert audio .mp4 → .mp3 with loudness normalisation
  4. Force Rec.709 colour metadata on the video-only stream
  5. Mux video + audio → _FINAL.mp4
  6. Clean up intermediate files (_VIDONLY, _AUDIO)

Fallback: if either stream is missing, yt-dlp fetches the best pre-muxed file.

Pipeline for Vimeo:
  yt-dlp is used with the embedded iframe src and the referrer page URL.
"""

import os
import ffmpeg
import yt_dlp
from pathlib import Path

from typing import List, Optional

RED    = "\033[31m"
YELLOW = "\033[33m"
RESET  = "\033[0m"

# Shared download-result accumulators (populated by callers)
SUCCESS_DOWNLOADS: List[dict] = []
FAILED_DOWNLOADS:  List[dict] = []


# ---------------------------------------------------------------------------
# Public entry point
# ---------------------------------------------------------------------------

def download_iframe_video(
    iframe_src: str,
    page_url: str,
    project_dir: Path,
    artist_name: str,
    art_title: str,
    project_id: int,
    verbose: bool = False,
) -> List[str]:
    """
    Download a video embedded in an <iframe> and return a list of relative
    Unity asset paths for every file written to disk.

    Args:
        iframe_src:   The `src` attribute of the <iframe> tag.
        page_url:     The URL of the page that contains the <iframe>
                      (needed as a Vimeo referrer).
        project_dir:  Absolute directory where media files are written.
        artist_name:  Used to derive file names.
        art_title:    Used to derive file names.
        project_id:   Integer hash used to build Unity-relative paths.
        verbose:      Print detailed progress messages.

    Returns:
        List of relative path strings (Assets/Artwork/<project_id>/…).
        Empty list if nothing was successfully downloaded.
    """
    if "youtube" in iframe_src:
        return _download_youtube(
            iframe_src, project_dir, artist_name, art_title, project_id, verbose
        )
    elif "vimeo" in iframe_src:
        return _download_vimeo(
            iframe_src, page_url, project_dir, artist_name, art_title, project_id, verbose
        )
    else:
        print(
            RED + "[ERROR] Unrecognised iframe host — cannot download video." + RESET
            + YELLOW + " Video NOT added." + RESET
        )
        return []


# ---------------------------------------------------------------------------
# YouTube
# ---------------------------------------------------------------------------

def _safe_destination(dest_dir: Path, filename: str) -> Path:
    """Return a unique path inside dest_dir, appending -1, -2 … if needed."""
    dest_dir.mkdir(parents=True, exist_ok=True)
    base      = Path(filename).stem
    ext       = Path(filename).suffix
    candidate = dest_dir / f"{base}{ext}"
    i = 1
    while candidate.exists():
        candidate = dest_dir / f"{base}-{i}{ext}"
        i += 1
    return candidate


def _download_youtube(
    iframe_src: str,
    project_dir: Path,
    artist_name: str,
    art_title: str,
    project_id: int,
    verbose: bool,
) -> List[str]:
    """Full YouTube download pipeline: separate video + audio → mux → _FINAL."""

    link = "https://www.youtube.com/watch?v=" + iframe_src.split("/")[-1].split("?")[0]

    safe_artist = _safe_name(artist_name)
    safe_title  = _safe_name(art_title)

    base_final    = f"{safe_artist}_{safe_title}_FINAL.mp4"
    base_vidonly  = f"{safe_artist}_{safe_title}_VIDONLY.mp4"
    base_audio    = f"{safe_artist}_{safe_title}_AUDIO.mp4"

    abs_final   = _safe_destination(project_dir, base_final)
    abs_vidonly = _safe_destination(project_dir, base_vidonly)
    abs_audio   = _safe_destination(project_dir, base_audio)

    rel_final   = Path("Assets") / "Artwork" / str(project_id) / abs_final.name
    rel_vidonly = Path("Assets") / "Artwork" / str(project_id) / abs_vidonly.name

    file_paths: List[str] = []

    # ── 1. Video-only ────────────────────────────────────────────────────────
    video_ok = _yt_download_video_only(link, abs_vidonly, verbose)
    if video_ok:
        file_paths.append(str(rel_vidonly))

    # ── 2. Force Rec.709 on video-only stream ────────────────────────────────
    if video_ok and abs_vidonly.exists():
        _force_rec709_video(abs_vidonly, verbose=verbose)

    # ── 3. Audio-only → mp3 ──────────────────────────────────────────────────
    abs_audio_final = _yt_download_audio_only(link, abs_audio, verbose)  # may return .mp3 path

    if abs_audio_final:
        rel_audio = Path("Assets") / "Artwork" / str(project_id) / abs_audio_final.name
        file_paths.append(str(rel_audio))

    # ── 4. Mux video + audio → _FINAL ────────────────────────────────────────
    muxed = _mux_video_audio(
        video_path  = abs_vidonly,
        audio_path  = abs_audio_final,
        output_path = abs_final,
        link        = link,
        verbose     = verbose,
    )

    if muxed:
        # Remove intermediates only when _FINAL exists and is non-empty
        if abs_final.exists() and abs_final.stat().st_size > 0:
            _safe_remove(abs_vidonly)
            if abs_audio_final:
                _safe_remove(abs_audio_final)
            # Also try .mp4 variant in case mp3 conversion failed
            _safe_remove(abs_audio.with_suffix(".mp4"))
        file_paths.append(str(rel_final))
    else:
        # Fallback: best pre-muxed single file
        _yt_download_best_combined(link, abs_final, verbose)
        if abs_final.exists() and abs_final.stat().st_size > 0:
            _safe_remove(abs_vidonly)
            if abs_audio_final:
                _safe_remove(abs_audio_final)
        file_paths.append(str(rel_final))

    # ── 5. Clean up any paths that no longer exist on disk ───────────────────
    file_paths = [
        p for p in file_paths
        if (project_dir.parent / p).exists()
    ]

    return file_paths


def _yt_download_video_only(link: str, destination: Path, verbose: bool) -> bool:
    """Download the best video-only stream. Returns True on success."""
    try:
        if verbose:
            print(f"[YT] Downloading VIDEO ONLY: {link}")

        opts = {
            "format":       "bv/b",
            "outtmpl":      str(destination),
            "quiet":        not verbose,
            "retries":      5,
            "ignoreerrors": False,
            "noplaylist":   True,
        }
        with yt_dlp.YoutubeDL(opts) as ydl:
            ydl.download(link)

        SUCCESS_DOWNLOADS.append({"url": link, "stage": "video-only", "dest": str(destination)})
        if verbose:
            print(f"[YT] VIDEO ONLY saved → {destination.name}")
        return True

    except Exception as e:
        FAILED_DOWNLOADS.append({"url": link, "stage": "video-only", "error": str(e)})
        if verbose:
            print(RED + f"[YT ERROR] VIDEO ONLY failed: {e}" + RESET)
        return False


def _yt_download_audio_only(link: str, destination: Path, verbose: bool) -> Optional[Path]:
    """
    Download the best audio-only stream, then convert to MP3.

    Returns the final audio Path (*.mp3 if conversion succeeded, else *.mp4),
    or None if the download itself failed.
    """
    try:
        if verbose:
            print(f"[YT] Downloading AUDIO ONLY: {link}")

        opts = {
            "format":       "ba/b",
            "outtmpl":      str(destination),
            "quiet":        not verbose,
            "retries":      5,
            "noplaylist":   True,
            "ignoreerrors": False,
        }
        with yt_dlp.YoutubeDL(opts) as ydl:
            ydl.download(link)

        if verbose:
            print(f"[YT] AUDIO ONLY saved → {destination.name}")

        # Convert to MP3 with loudness normalisation
        mp3_path = destination.with_suffix(".mp3")
        try:
            (
                ffmpeg
                .input(str(destination))
                .output(
                    str(mp3_path),
                    acodec        = "mp3",
                    audio_bitrate = "192k",
                    af            = "loudnorm=I=-14:TP=-1.5:LRA=11",
                )
                .overwrite_output()
                .run(quiet=not verbose)
            )
            os.remove(destination)
            SUCCESS_DOWNLOADS.append({"url": link, "stage": "audio-mp3", "dest": str(mp3_path)})
            if verbose:
                print(f"[YT] Converted audio → {mp3_path.name}")
            return mp3_path

        except Exception as e:
            if verbose:
                print(RED + f"[YT] MP3 conversion failed, keeping .mp4 audio: {e}" + RESET)
            return destination  # fallback: raw .mp4 audio

    except Exception as e:
        FAILED_DOWNLOADS.append({"url": link, "stage": "audio-only", "error": str(e)})
        if verbose:
            print(RED + f"[YT ERROR] AUDIO ONLY failed: {e}" + RESET)
        return None


def _yt_download_best_combined(link: str, destination: Path, verbose: bool) -> None:
    """Fallback: download the best pre-muxed (combined) stream."""
    try:
        opts = {
            "format":       "b",
            "outtmpl":      str(destination),
            "quiet":        not verbose,
            "retries":      5,
            "noplaylist":   True,
            "ignoreerrors": False,
        }
        with yt_dlp.YoutubeDL(opts) as ydl:
            ydl.download(link)
        SUCCESS_DOWNLOADS.append({"url": link, "stage": "precombined", "dest": str(destination)})
        if verbose:
            print(f"[YT] Downloaded best pre-combined → {destination.name}")

    except Exception as e:
        FAILED_DOWNLOADS.append({"url": link, "stage": "precombined", "error": str(e)})
        if verbose:
            print(RED + f"[YT ERROR] Pre-combined download failed: {e}" + RESET)


def _mux_video_audio(
    video_path: Path,
    audio_path: Optional[Path],
    output_path: Path,
    link: str,
    verbose: bool,
) -> bool:
    """
    Mux a separate video and audio file into a single MP4 with Rec.709 metadata.

    Returns True if the output file was created successfully.
    """
    if not (video_path and video_path.exists()):
        if verbose:
            print(YELLOW + "[MUX] Video-only file missing — skipping mux." + RESET)
        return False

    if not (audio_path and audio_path.exists()):
        if verbose:
            print(YELLOW + "[MUX] Audio file missing — skipping mux." + RESET)
        return False

    try:
        if verbose:
            print(f"[MUX] Combining {video_path.name} + {audio_path.name} → {output_path.name}")

        video_in = ffmpeg.input(str(video_path))
        audio_in = ffmpeg.input(str(audio_path))

        ffmpeg.output(
            audio_in, video_in,
            str(output_path),
            vcodec         = "copy",
            acodec         = "copy",
            format         = "mp4",
            color_primaries= "bt709",
            color_trc      = "bt709",
            colorspace     = "bt709",
        ).run(overwrite_output=True, quiet=not verbose)

        if output_path.exists() and output_path.stat().st_size > 0:
            SUCCESS_DOWNLOADS.append({"url": link, "stage": "mux", "dest": str(output_path)})
            if verbose:
                print(f"[MUX] Success → {output_path.name}")
            return True

        return False

    except Exception as e:
        FAILED_DOWNLOADS.append({"url": link, "stage": "mux", "error": str(e)})
        if verbose:
            print(RED + f"[MUX ERROR] {e}" + RESET)
        return False


def _force_rec709_video(mp4_path: Path, verbose: bool = True) -> None:
    """
    Re-encode a video-only MP4 to embed Rec.709 colour metadata.

    Writes to a temp file then swaps in-place.
    """
    temp = mp4_path.with_name(mp4_path.stem + "_rec709_temp.mp4")
    try:
        if verbose:
            print(f"[REC709] Applying bt709 metadata to {mp4_path.name}")

        (
            ffmpeg
            .input(str(mp4_path))
            .output(
                str(temp),
                vcodec         = "libx264",
                pix_fmt        = "yuv420p",
                color_primaries= "bt709",
                color_trc      = "bt709",
                colorspace     = "bt709",
                acodec         = "copy",  # video-only — no audio stream
            )
            .overwrite_output()
            .run(quiet=not verbose)
        )

        if temp.exists() and temp.stat().st_size > 0:
            os.remove(mp4_path)
            temp.rename(mp4_path)
            if verbose:
                print(f"[REC709] Done → {mp4_path.name}")
        else:
            if verbose:
                print(YELLOW + f"[REC709] Temp file empty — skipping swap for {mp4_path.name}" + RESET)

    except Exception as e:
        if verbose:
            print(RED + f"[REC709 ERROR] {mp4_path.name}: {e}" + RESET)
        _safe_remove(temp)


# ---------------------------------------------------------------------------
# Vimeo
# ---------------------------------------------------------------------------

def _download_vimeo(
    iframe_src: str,
    page_url: str,
    project_dir: Path,
    artist_name: str,
    art_title: str,
    project_id: int,
    verbose: bool,
) -> List[str]:
    """Download a Vimeo video using yt-dlp with the page URL as referer."""

    base_video = f"{_safe_name(artist_name)}_{_safe_name(art_title)}.mp4"
    abs_dest   = _safe_destination(project_dir, base_video)
    rel_dest   = Path("Assets") / "Artwork" / str(project_id) / abs_dest.name

    try:
        if verbose:
            print(f"[VIMEO] Downloading {iframe_src}")

        opts = {
            "format":  "bestvideo+bestaudio/best",
            "outtmpl": str(abs_dest),
            "quiet":   not verbose,
            "retries": 5,
            "http_headers": {"Referer": page_url},
        }
        with yt_dlp.YoutubeDL(opts) as ydl:
            ydl.download([iframe_src])

        if verbose:
            print(f"[VIMEO] Downloaded → {abs_dest.name}")

        SUCCESS_DOWNLOADS.append({"url": iframe_src, "stage": "vimeo", "dest": str(rel_dest)})
        return [str(rel_dest)]

    except Exception as e:
        FAILED_DOWNLOADS.append({"url": iframe_src, "stage": "vimeo", "error": str(e)})
        if verbose:
            print(RED + f"[VIMEO ERROR] {iframe_src}: {e}" + RESET)
        return []


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _safe_name(s: str) -> str:
    """Strip spaces for use in file names."""
    return s.replace(" ", "")


def _safe_remove(path: Optional[Path]) -> None:
    """Delete a file silently if it exists."""
    try:
        if path and Path(path).exists():
            os.remove(path)
    except Exception:
        pass