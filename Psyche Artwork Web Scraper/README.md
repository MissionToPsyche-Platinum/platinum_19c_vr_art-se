## Running the Psyche Artwork Web Scraper

To run **`PsycheScraper.py`**, you’ll need to have **Python** installed.  
You can download it from the official website:  
👉 https://www.python.org/downloads/

Installing Python like this should also install **pip**, which is needed to install required libraries.

### 1. Verify installation
Open a terminal (or command prompt) and check that both Python and pip are installed:
```bash
python --version
pip --version
```


### 2. Install Dependencies
Once verified, install the required libraries by running the following commands:
```bash
pip install -r requirements.txt
```
### 3. FFMPEG Installation
ffmpeg requires a bit more work to get functioning.\
Windows-
1. Download the ffmpeg labeled "latest git master branch build" from the following site (https://www.gyan.dev/ffmpeg/builds/). Simply using pip install ffmpeg WILL NOT WORK, as it does not contain the executable file required.
2. Extract (unzip) the file from wherever you downloaded it.
3. (OPTIONAL) You may want to move the extracted file to your 'Program Files' folder (located within your 'Local Disk (C:)'.)
4. Press WINDOWS + R and then type sysdm.cpl into the field. This should open the system properties window.
5. Select the 'Advanced' tab (3rd), then click 'Enviroment Variables'
6. Click on the row labeled 'Path' then click 'Edit'. This should display a new screen.
7. Return to where you your extracted ffmpeg file is, and enter it. Select the folder named 'bin', right-click it, and select Copy as Path (or use Ctrl + Shift + C)
8. Return to the 'Edit Enviroment Variables window that is open, and click New. This should highlight a new row, and allow you to paste the path into it (Ctrl + V).
9. Make sure to hit 'OK' on both windows to set the paths.
10. In your terminal, run the command `pip install ffmpeg-python`
11. Try rerunning the program. You may need to reboot the terminal for the new path to take effect.

MacOS-

Linux-

You should be good to go!