import os
import requests
import bs4
from bs4 import BeautifulSoup
from pathlib import Path
from typing import List

import pandas as pd

from src.media_handling.image_downloader import download_image, ALLOWED_IMAGE_EXTENSIONS
from src.media_handling.pdf_downloader import download_pdf
from src.media_handling.video_downloader import download_iframe_video

RED    = "\033[31m"
YELLOW = "\033[33m"
RESET  = "\033[0m"

def get_art_filepath(row:pd.Series, verbose:bool = False)-> List[str]:
    url = row["Project Link"].strip()
    artist_name = row["Artist Name"].strip()
    art_title = row["Project Title"].strip()

    project_id = make_project_id(artist_name, art_title)
    project_dir = get_artwork_dir() / str(project_id)
    project_dir.mkdir(parents=True, exist_ok=True)

    try:
        page = requests.get(url, timeout=30)
    except requests.exceptions.RequestException as e:
        print(RED + f"[SCRAPER ERROR] Could not fetch {url}: {e}" + RESET)
        return []

    soup = BeautifulSoup(page.text, "html.parser")
    file_paths: List[str] = []

    # ── Embedded iframe (YouTube / Vimeo) ─────────────────────────────────────
    gallery = soup.find("div", class_="gallery-slide")
    iframe_tag = gallery.find("iframe") if gallery else None

    if isinstance(iframe_tag, bs4.Tag) and iframe_tag.has_attr("src"):
        file_paths = download_iframe_video(
            iframe_src=iframe_tag["src"],
            page_url=url,
            project_dir=project_dir,
            artist_name=artist_name,
            art_title=art_title,
            project_id=project_id,
            verbose=verbose,
        )

    else:
        # ── Direct download links ─────────────────────────────────────────────
        download_links: List[str] = []
        for tag in soup.find_all("a"):
            if tag.get("class") == ["link", "mb-5", "mr-sm"]:
                raw = tag.get("data-downloads", "")
                download_links = raw.split(",") if "," in raw else ([raw] if raw else [])

        for link in download_links:
            link = link.strip()
            if not link:
                continue

            try:
                resp = requests.get(link, timeout=30)
                if resp.status_code != 200:
                    if verbose:
                        print(RED + f"[SCRAPER ERROR] Bad response ({resp.status_code}) for {link}" + RESET)
                    continue

                ext = Path(link.split("/")[-1]).suffix.lower()
                abs_dest = _safe_destination(project_dir, link.split("/")[-1])

                if ext in ALLOWED_IMAGE_EXTENSIONS:
                    result = download_image(resp, abs_dest, verbose=verbose)
                    if result:
                        file_paths.append(
                            str(Path("Assets") / "Artwork" / str(project_id) / Path(result).name)
                        )

                elif ext == ".pdf":
                    result = download_pdf(resp, abs_dest, verbose=verbose)
                    if result:
                        file_paths.append(
                            str(Path("Assets") / "Artwork" / str(project_id) / Path(result).name)
                        )

                else:
                    print(
                        RED + f"[SCRAPER ERROR] Unsupported file type '{ext}': {link}" + RESET
                        + YELLOW + " File NOT added." + RESET
                    )

            except requests.exceptions.RequestException as e:
                if verbose:
                    print(RED + f"[SCRAPER ERROR] Could not download {link}: {e}" + RESET)

    # Strip paths whose files no longer exist on disk (cleaned up after muxing)
    file_paths = [
        p for p in file_paths
        if (project_dir.parent.parent / p).exists()
    ]

    return file_paths


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def get_artwork_dir() -> Path:
    artwork_dir = Path(os.getenv("OUTPUT_PATH")) / "Artwork"
    artwork_dir.mkdir(parents=True, exist_ok=True)
    return artwork_dir


def make_project_id(artist_name: str, artwork_title: str) -> int:
    import hashlib
    seed = f"project::{artist_name.strip().lower()}::{artwork_title.strip().lower()}"
    h = hashlib.sha256(seed.encode("utf-8")).hexdigest()
    return int(h[:16], 16) & ((1 << 63) - 1)


def _safe_destination(dest_dir: Path, filename: str) -> Path:
    dest_dir.mkdir(parents=True, exist_ok=True)
    base = Path(filename).stem
    ext = Path(filename).suffix
    candidate = dest_dir / f"{base}{ext}"
    i = 1
    while candidate.exists():
        candidate = dest_dir / f"{base}-{i}{ext}"
        i += 1
    return candidate
