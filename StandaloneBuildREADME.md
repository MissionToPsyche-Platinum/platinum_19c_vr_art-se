## Running the Standalone Installer

In order to fully install the museum, do the following:

1. Install the project repository using the command `git clone https://github.com/MissionToPsyche-Platinum/platinum_19c_vr_art-se.git`
2. Run the web scraper before running this script.  Tools and instructions on how to do this can be found in the Psyche Artwork Web Scraper folder in the project directory.
3. Install ADB from [this website](https://developer.android.com/tools/releases/platform-tools) if you don't already have it and add it to system path.
4. Install Unity if you don't already have it.  To do this, you must install the Unity Hub from [this website](https://docs.unity.com/en-us/hub/install-hub).  Launch the Unity Hub and download Unity from the Installs page.  Our project runs on Unity 6000.2.10f1.
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
	> ⚠️ This will result in your computer allowing PowerShell scripts from any source to be run.  If you would like to revert this setting to its original value after installing the museum program, run the command `Set-ExecutionPolicy Restricted` after you have finished the rest of the steps.
10. IF BUILDING FROM ARTWORK FOLDER: Make sure Unity isn't running, and then Run `.\Update_Artwork.ps1 -B` in this directory. Otherwise just running `.\Update_Artwork.ps1` with a valid Bundles folder will install the bundles to the headset.
	* If asked, select the "Run once" option in the terminal. 
	* The "-B" variant will take around 10 minutes, be patient! 

Once the script finishes, the database should be ready to go on the headset! There's also build functionality in the script if it's run in the project directory, to where given an Artwork and Database folder, a full fresh install of the database can be automatically done. For the purposes of installing the project, the above should suffice.
Building the project causes a Bundles directory to be generated in the project root. When sending the project to others to install, this directory is vital along with the Database folder to install the artwork for the museum to display.
Presently, there's a minor bug where an extra directory is sometimes produced in the Bundles directory, this is not intended. 