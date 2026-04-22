#PATHS
$artworkPath = "./ART_DATABASE/Artwork" #change this to the path of the artwork folder, only necessary if building
$bundleSource = "./Bundles" #change this to the path of the build bundle folder, keep as ./Bundles if building from Artwork
$databasePath = "./ART_DATABASE/Database" #change this to the path of the database folder, always necessary

# Config
$bundleBuild = "./Psyche VR Experience\ServerData\Android"
$packageName  = "com.PlatinumPsycheTeam19.VRArtMuseum"
$deviceDest   = "/sdcard/Android/data/$packageName/files/aa/Android"
$deviceDB = "/sdcard/Android/data/$packageName/files/Database"
$adb          = "adb"  # or full path like "C:\platform-tools\adb.exe"
$unityExe  = "C:\Program Files\Unity\Hub\Editor\6000.2.10f1\Editor\Unity.exe" #change this to your Unity.exe path



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

    $unityArgs = @(
        "-batchmode",
    	"-projectPath", "`"./Psyche VR Experience/`"",
    	"-executeMethod", "BuildScript.BuildAddressables",
        "-artworkPath", $artworkPath,
	"-logFile", "unity_build.log",
        "-quit"
    )
    
    $process = Start-Process -FilePath $unityExe -ArgumentList $unityArgs -Wait -PassThru
    $exitCode = $process.ExitCode

    $end = Get-Date
    $duration = $end - $start

    if ($exitCode -eq 0) {
        Write-Host "Build completed at $end (took $($duration.ToString('mm\:ss')))" -ForegroundColor Green
	Move-Item -Path $bundleBuild -Destination $bundleSource
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