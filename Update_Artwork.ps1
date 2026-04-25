#PATHS
$artworkPath = "C:\Artwork" #change this to the path of the artwork folder, only necessary if building
$bundleSource = "./Bundles" #change this to the path of the build bundle folder, keep as ./Bundles if building from Artwork as this script will put a Bundles folder in the same directory as it was run in when -B is an arg
$databasePath = "C:\Database" #change this to the path of the database folder, necessary when pushing to headset
$unityExe  = "C:\Program Files\Unity\Hub\Editor\6000.2.10f1\Editor\Unity.exe" #change this to your Unity.exe path

# Config
$bundleBuild = "./Psyche VR Experience\ServerData\Android\*" #the spot to which the bundles are built in Unity, used to extract and copy them to the root directory
$packageName  = "com.PlatinumPsycheTeam19.VRArtMuseum" #com.CompanyName.Program structure within Unity, used when determining the persistent data path
$deviceDest   = "/sdcard/Android/data/$packageName/files/aa/Android" #the spot on the headset where the artwork is pushed
$deviceDB = "/sdcard/Android/data/$packageName/files/Database" #the spot on the headset where the database is pushed
$adb          = "adb"  # or full path like "C:\platform-tools\adb.exe", if system paths are set up properly leave this as "adb"

function Push-Bundles {
    Write-Host "Checking for connected device..."
    $device = & $adb devices | Select-String -Pattern "^\S+\s+device$"
    if (-not $device) {
        Write-Error "No device connected."
        return
    }

    Write-Host "Pushing bundles from $bundleSource..."
    & $adb shell "mkdir -p $deviceDest"
    & $adb push "$bundleSource\." $deviceDest
    & $adb shell "mkdir -p $deviceDB"
    & $adb push "$databasePath\." $deviceDB

    if ($LASTEXITCODE -eq 0) {
        Write-Host "Push complete." -ForegroundColor Green
    } else {
        Write-Error "Push failed."
    }
}

function Build-AndPush {
    $start = Get-Date
    Write-Host "Build started at $start"

    Remove-Item $bundleBuild -recurse -Force

    $unityArgs = @(
        "-batchmode",
    	"-projectPath", "`"./Psyche VR Experience/`"",
    	"-executeMethod", "BuildScript.BuildAddressables",
        "-artworkPath", "`"$artworkPath`"",
	"-logFile", "unity_build.log",
        "-quit"
    )
    
    $process = Start-Process -FilePath $unityExe -ArgumentList $unityArgs -Wait -PassThru
    $exitCode = $process.ExitCode

    $end = Get-Date
    $duration = $end - $start

    if ($exitCode -eq 0) {
        Write-Host "Build completed at $end (took $($duration.ToString('mm\:ss')))" -ForegroundColor Green
	Remove-Item $bundleSource -recurse -Force
	New-Item $bundleSource -ItemType Directory
	Copy-Item -Path $bundleBuild -Destination "$bundleSource/" -recurse -Force
        Push-Bundles
    } else {
        Write-Host "Build failed after $($duration.ToString('mm\:ss'))" -ForegroundColor Red
    }
}

# entry point
if ($args -contains "-B") {
    Build-AndPush
} else {
    Push-Bundles
}