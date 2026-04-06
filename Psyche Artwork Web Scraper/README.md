## Running the Psyche Artwork Web Scraper

To run the web scraper, you’ll need to have **Docker** installed.  
There are many ways to download it, but here is the official guides:  
👉 Windows: https://docs.docker.com/desktop/setup/install/windows-install/  
👉 Mac: https://docs.docker.com/desktop/setup/install/mac-install/  
👉 Linux: https://docs.docker.com/desktop/setup/install/linux/  

Installing Python like this should also install **pip**, which is needed to install required libraries.

### 1. Verify installation
Open a terminal (or command prompt), start Docker and check that the installation worked properly:
```bash
docker --version
```
If you see an error or no output, Docker may not be installed correctly — revisit the installation steps.  

### 2. Run the Scraper with Docker

**Prerequisites**
1. Start Docker Desktop (or the Docker daemon)
2. Create an input folder and populate it with your CSV file and art pieces (if modifying existing projects)
3. Create an output folder for the results
4. Copy `.env.example` to a new file named `.env` and update `INPUT_PATH` and `OUTPUT_PATH` to point to your chosen folders

> ⚠️ The `.env` file is required — the scraper will not run without it.

**Running the Program**

Build the Docker image:
```bash
docker compose build
```

Run the scraper (automatically removed when finished):
```bash
docker compose run --rm psyche-art-scraper
```