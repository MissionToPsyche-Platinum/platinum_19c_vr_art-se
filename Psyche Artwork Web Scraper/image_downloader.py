"""
image_downloader.py
-------------------
Handles downloading standard image files and converting GIFs to MP4
for Unity VideoPlayer compatibility.
"""

import os
from typing import Optional

import ffmpeg
from pathlib import Path

RED    = "\033[31m"
YELLOW = "\033[33m"
RESET  = "\033[0m"

# Image types Unity can import directly
ALLOWED_IMAGE_EXTENSIONS = {
    ".bmp", ".exr", ".gif", ".hdr", ".iff",
    ".jpeg", ".jpg", ".pct", ".pic", ".pict",
    ".png", ".psd", ".tga", ".tif", ".tiff",
}


def download_image(response, destination: Path, verbose: bool = False) -> Optional[str]:
    """
    Save a raw image response to disk.

    GIFs are automatically converted to MP4 (Unity VideoPlayer does not support
    animated GIFs natively).  All other allowed image types are written as-is.

    Args:
        response:    A requests.Response whose .content is raw image bytes.
        destination: Absolute path where the file should be written.
        verbose:     If True, print progress and error details.

    Returns:
        A relative Unity asset path string on success, or None on unsupported type.
    """
    destination = Path(destination)
    ext         = destination.suffix.lower()

    if ext not in ALLOWED_IMAGE_EXTENSIONS:
        if verbose:
            print(RED + f"[ERROR] Unsupported image extension: {ext}" + RESET)
        return None

    try:
        with open(destination, "wb") as f:
            f.write(response.content)

        if verbose:
            print(f"[IMAGE] Saved {destination.name}")

        # GIFs must be converted to MP4 for Unity
        if ext == ".gif":
            converted = _convert_gif_to_mp4(destination, verbose=verbose)
            return str(converted)

        return str(destination)

    except Exception as e:
        if verbose:
            print(RED + f"[ERROR] Failed to save image {destination.name}: {e}" + RESET)
        return None


# ---------------------------------------------------------------------------
# GIF → MP4 conversion (Unity VideoPlayer compatibility)
# ---------------------------------------------------------------------------

def _convert_gif_to_mp4(gif_path: Path, verbose: bool = True, fps: int = 24) -> Path:
    """
    Convert an animated GIF to an H.264 MP4 with Rec.709 colour metadata.

    The source GIF is deleted after a successful conversion.

    Args:
        gif_path: Absolute path to the .gif file.
        verbose:  Print progress messages.
        fps:      Target frame rate for the output video (default 24).

    Returns:
        Path to the resulting .mp4, or the original gif_path if conversion failed.
    """
    gif_path = Path(gif_path)
    mp4_path = gif_path.with_name(gif_path.stem + "_GIF.mp4")

    try:
        if verbose:
            print(f"[GIF→MP4] {gif_path.name} → {mp4_path.name}")

        # Force even dimensions (required by H.264) and constant frame rate
        vf = f"fps={fps},scale=trunc(iw/2)*2:trunc(ih/2)*2:flags=lanczos"

        (
            ffmpeg
            .input(str(gif_path))
            .output(
                str(mp4_path),
                vcodec         = "libx264",
                pix_fmt        = "yuv420p",
                vf             = vf,
                r              = fps,
                vsync          = "cfr",
                an             = None,          # no audio track
                movflags       = "+faststart",
                **{
                    "profile:v":       "baseline",
                    "level":           "3.0",
                    "crf":             "18",
                    "preset":          "slow",
                    "color_primaries": "bt709",
                    "color_trc":       "bt709",
                    "colorspace":      "bt709",
                    "tag:v":           "avc1",
                }
            )
            .overwrite_output()
            .run(quiet=not verbose)
        )

        if mp4_path.exists() and mp4_path.stat().st_size > 0:
            try:
                gif_path.unlink()
            except Exception as e:
                if verbose:
                    print(f"[GIF→MP4] Warning: couldn't delete source GIF: {e}")

            # Second pass to guarantee bt709 metadata is embedded correctly
            _force_rec709(mp4_path, verbose=verbose, fps=fps)
            return mp4_path

        if verbose:
            print("[GIF→MP4] Output missing/empty — keeping original GIF.")
        return gif_path

    except Exception as e:
        if verbose:
            print(RED + f"[GIF→MP4 ERROR] {gif_path}: {e}" + RESET)
        return gif_path


def _force_rec709(mp4_path: Path, verbose: bool = True, fps: int = 24) -> Path:
    """
    Re-encode an MP4 to guarantee Rec.709 colour primaries/trc/colorspace metadata.

    Unity raises colour-space warnings when these are absent or set to unknown.
    This function writes to a temporary file and swaps it in-place so the
    caller's path remains valid.

    Args:
        mp4_path: Absolute path to an existing .mp4 file.
        verbose:  Print progress messages.
        fps:      Frame rate to enforce in the output.

    Returns:
        The same mp4_path (now re-encoded in place).
    """
    mp4_path = Path(mp4_path)
    if mp4_path.suffix.lower() != ".mp4":
        return mp4_path

    temp = mp4_path.with_name(mp4_path.stem + "_rec709fix_temp.mp4")

    try:
        if verbose:
            print(f"[REC709 FIX] {mp4_path.name}")

        vf = f"fps={fps},scale=trunc(iw/2)*2:trunc(ih/2)*2"

        (
            ffmpeg
            .input(str(mp4_path))
            .output(
                str(temp),
                vcodec         = "libx264",
                pix_fmt        = "yuv420p",
                vf             = vf,
                r              = fps,
                vsync          = "cfr",
                an             = None,
                movflags       = "+faststart",
                **{
                    "profile:v":       "baseline",
                    "level":           "3.0",
                    "crf":             "18",
                    "preset":          "slow",
                    "color_primaries": "bt709",
                    "color_trc":       "bt709",
                    "colorspace":      "bt709",
                    "color_range":     "tv",
                    "tag:v":           "avc1",
                }
            )
            .overwrite_output()
            .run(quiet=not verbose)
        )

        if temp.exists() and temp.stat().st_size > 0:
            os.remove(mp4_path)
            temp.rename(mp4_path)
            if verbose:
                print(f"[REC709 FIX] Applied bt709 metadata to {mp4_path.name}")
        else:
            if verbose:
                print(f"[REC709 FIX] Temp output missing/empty for {mp4_path.name}")

    except Exception as e:
        if verbose:
            print(RED + f"[REC709 FIX ERROR] {mp4_path.name}: {e}" + RESET)
        try:
            if temp.exists():
                temp.unlink()
        except Exception:
            pass

    return mp4_path