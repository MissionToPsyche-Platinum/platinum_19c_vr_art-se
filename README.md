# Psyche Inspired Virtual Reality Museum
Welcome to the Psyche VR Museum repository!  This is a VR museum built in fulfillment of the ASU Captstone Course SER401/402 for our sponsor, the NASA Psyche Mission.  Art pieces from students participating in the Psyche Inspired program are displayed in a fantastical procedurally-generated museum.  This repo contains the source code for the museum, the code to download all artwork projects for the museum, and a script to install the standalone museum on a headset.  Instructions for each are given below.  This project was developed in Unity for the Meta Quest virtual reality headset.  We hope you enjoy the museum!

## Running the Psyche Artwork Web Scraper

This web scraper will download all needed artwork information and assets from the Psyche website into a format that is usable by the museum.  This program must be run before the museum can be launched.

To run the web scraper, you’ll need to have **Docker** installed.  
There are many ways to download it, but here is the official guides:  
👉 Windows: https://docs.docker.com/desktop/setup/install/windows-install/  
👉 Mac: https://docs.docker.com/desktop/setup/install/mac-install/  
👉 Linux: https://docs.docker.com/desktop/setup/install/linux/  

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

## Notes

This program takes a long time to run.  Do not worry if it is still running after an hour.

Some of the libraries used may show warning or error messages in the terminal.  These are normal and will not negatively affect the program's outcome.

When using the adding and modifying functionality, keep in mind that any project media you would like to add must be on of the following file types in order to load into the VR museum: 
- .bmp
- .exr
- .gif
- .hdr
- .iff
- .jpeg
- .jpg
- .mp4
- .pct
- .pic
- .pict
- .pdf
- .png
- .psd
- .tga
- .tif
- .tiff

## Running the Standalone Installer

In order to fully install the museum, do the following:

1. Install the project repository using the command `git clone https://github.com/MissionToPsyche-Platinum/platinum_19c_vr_art-se.git`
2. Run the web scraper before running this script.  Tools and instructions on how to do this can be found in the Psyche Artwork Web Scraper folder in the project directory.
3. Install ADB from [this website](https://developer.android.com/tools/releases/platform-tools) if you don't already have it and add it to system path.
4. Install Unity if you don't already have it.  To do this, you must install the Unity Hub from [this website](https://docs.unity.com/en-us/hub/install-hub).  Launch the Unity Hub and download Unity from the Installs page.  Our project runs on Unity 6000.2.10f1. You must also install Android Build Support, which can be done by opening Unity Hub -> Installs and on Unity 6.2, clicking Manage -> Add Modules and installing Android Build Support.
5. Plug in the Quest VR headset.
6. Install the APK onto the headset. [`adb install -r PsycheVRMuseum.apk`]
7. Open the Update_Artworks.ps1 file in a text editor.  Depending on your use case, you will need to change different variables. It is recommended to use absolute paths, like "C:/Folder/Artwork".
	* `artworkPath`: If you are building the project, update this variable to the path of the Artwork folder produced by running the web scraper.
	* `bundleSource`: Change this to the path of the bundles build folder.  If you are building from Artwork, you can leave this as-is at the default `./Bundles` directory.
	* `databasePath`: Change this to the path of the Database folder produced by running the web scraper.  **This is always necessary.**
	* `adb`: Change this to the full path of the adb.exe file, such as "C:\platform-tools\adb.exe". This step is only necessary if you can't get the system path variable working properly.
	* `unityExe`: Change this to the Unity.exe path.  You can find this in the Unity Hub application on the Installs page.  The correct path will be listed on the entry for Unity 6000.2.10f1.
8. Run Powershell as an administrator in the project directory containing the file Update_Artwork.ps1.
9. Enter the command `Set-ExecutionPolicy Unrestricted` and select the "Yes to All" option.
	> ⚠️ This will result in your computer allowing PowerShell scripts from any source to be run, although you will still be asked to confirm manually each time before running.  If you would like to revert this setting to its original value after installing the museum program, run the command `Set-ExecutionPolicy Restricted` after you have finished the rest of the steps.
10. IF BUILDING FROM ARTWORK/DATABASE FOLDER: Make sure Unity isn't running, and then Run `.\Update_Artwork.ps1 -B` in this directory. Otherwise just running `.\Update_Artwork.ps1` with a valid Bundles folder will install the bundles to the headset.
	* If asked, select the "Run once" option in the terminal. 
	* The "-B" variant will take around 10 minutes, be patient!

Once the script finishes, the database should be ready to go on the headset! If strange things happen when the museum attempts to load art, uninstalling and reinstalling the apk and then rebuilding the art database usually fixes those problems.
Building the project causes a Bundles directory to be generated in the project root. When sending the project to others to install, this directory is vital along with the Database folder to install the artwork for the museum to display.

## Disclaimer
This work was created in partial fulfillment of ASU Capstone Course SER401/402. The work is a result of the Psyche Student Collaborations component of NASA’s Psyche Mission (https://psyche.ssl.berkeley.edu). “Psyche: A Journey to a Metal World” [Contract number NNM16AA09C] is part of the NASA Discovery Program mission to solar system targets. Trade names and trademarks of ASU and NASA are used in this work for identification only. Their usage does not constitute an official endorsement, either expressed or implied, by Arizona State University or National Aeronautics and Space Administration. The content is solely the responsibility of the authors and does not necessarily represent the official views of ASU or NASA.
