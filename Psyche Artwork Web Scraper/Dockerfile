# Dockerfile for Psyche Artwork Web Scraper
# Runs the interactive CLI from PsycheArtCLI.py

FROM python:3.12-slim

# Set a working directory
WORKDIR /app

# Install ffmpeg system package 
RUN apt-get update && apt-get install -y --no-install-recommends \
    ffmpeg \
    && rm -rf /var/lib/apt/lists/*

# Copy requirements and install deps first to leverage Docker layer caching
COPY requirements.txt ./
RUN pip install --no-cache-dir -r requirements.txt

# Copy application source
COPY . ./

ENV PYTHONPATH=/app

# Default command to run CLI; interactive mode
ENTRYPOINT ["python", "src/PsycheArtCLI.py"]
