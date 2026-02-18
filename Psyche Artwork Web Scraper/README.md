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
If nothing shows up or an error occurs, ensure you installed python properly

### 2. FFMPEG Installation
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
11. Now, to ensure it installed correctly, run the command `pip show ffmpeg-python`
    There should be a return that resembles the following:
```
Name: ffmpeg-python
Version: 0.2.0
Summary: Python bindings for FFmpeg - with complex filtering support
Home-page: https://github.com/kkroening/ffmpeg-python
Author: Karl Kroening
Author-email: karlk@kralnet.us
License: UNKNOWN
Location: C:\Users\user\AppData\Roaming\Python\Python314\site-packages
``` 
12. Navigate to the "Location" returned and find the `ffmpeg` directory.
    You are looking for a file named `_run.py`
    If it is present, no further work is necessary for this portion. If not, manually delete the ffmpeg directory and return to step 10.

### 3. Install Dependencies
Once verified, install the required libraries by running the following commands:
```bash
pip install -r requirements.txt --upgrade
```
This call requires the requirements.txt file to be in the active directory, so navigate to the project directory and open the command/bash prompt at the following location `(saved_location)/Psyche Artwork Web Scraper`

You should be good to go!