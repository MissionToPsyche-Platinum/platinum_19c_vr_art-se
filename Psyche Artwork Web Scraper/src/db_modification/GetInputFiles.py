from pathlib import Path
import os

def GetValidFilesInInputDirectory():
    ALLOWED_FILE_EXTENSIONS = {
        ".bmp", ".exr", ".gif", ".hdr", ".iff",
        ".jpeg", ".jpg", ".pct", ".pic", ".pict",
        ".png", ".psd", ".tga", ".tif", ".tiff", ".mp4"
    }
    
    inputDirectory = Path(os.getenv('INPUT_PATH'))
    return [f for f in inputDirectory.iterdir() if f.is_file() and f.suffix.lower() in ALLOWED_FILE_EXTENSIONS]