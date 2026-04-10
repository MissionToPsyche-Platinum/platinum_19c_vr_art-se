"""
pdf_downloader.py
-----------------
Handles downloading and converting PDF files to PNG images.
PDFs are converted to a single stacked PNG image for Unity compatibility.
"""

import fitz  # PyMuPDF
from PIL import Image
from io import BytesIO
from pathlib import Path

RED    = "\033[31m"
YELLOW = "\033[33m"
RESET  = "\033[0m"


def download_pdf(response, destination: Path, verbose: bool = False) -> str | None:
    """
    Convert a PDF (provided as a requests.Response) into a single stacked PNG.

    All pages of the PDF are rendered and stitched vertically into one image.
    The resulting PNG is saved at `destination` (with the suffix replaced by .png).

    Args:
        response:    A requests.Response whose .content is raw PDF bytes.
        destination: Intended file path (the suffix will be changed to .png).
        verbose:     If True, print progress and error details.

    Returns:
        The absolute path string of the saved PNG on success, or None on failure.
    """
    try:
        destination = Path(destination).with_suffix(".png")

        # Open the PDF from memory — avoids a redundant disk write
        pdf = fitz.open(stream=response.content, filetype="pdf")
        images = []

        # Render each page to a PIL Image
        for page_index in range(pdf.page_count):
            page = pdf.load_page(page_index)
            pix  = page.get_pixmap()
            img  = Image.open(BytesIO(pix.tobytes("png")))
            images.append(img)

        # Stack pages vertically into a single composite image
        total_height = sum(img.height for img in images)
        max_width    = max(img.width  for img in images)
        combined     = Image.new("RGB", (max_width, total_height), (255, 255, 255))

        y = 0
        for img in images:
            combined.paste(img, (0, y))
            y += img.height

        combined.save(destination)
        pdf.close()

        if verbose:
            print(f"[PDF] Converted {pdf.page_count} page(s) → {destination.name}")

        return str(destination)

    except Exception as e:
        if verbose:
            print(RED + "[ERROR] Failed to convert PDF to PNG." + RESET)
            print(RED + str(e) + RESET)
        return None