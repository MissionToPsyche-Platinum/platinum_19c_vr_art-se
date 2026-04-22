using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

//note: this is kinda vibe coded but if it works it works!
//      I still feel kinda gross about it...
public static class AddressablesSetup
{
    // ---- CONFIG ----
    private static string ArtworkInputPath
    {
        get
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-artworkPath")
                    return args[i + 1];
            }
            // Fallback default
            return @"C:\Artwork";
        }
    }
    private static readonly string ArtworkDestPath = "Assets/Artwork"; // inside Unity project
    // ----------------

    public static void MarkArtworkAddressable()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("Addressable Asset Settings not found. " +
                           "Create them via Window > Asset Management > Addressables > Groups.");
            return;
        }

        // Step 1 — Copy any new/updated folders from input path into project
        if (!Directory.Exists(ArtworkInputPath))
        {
            Debug.LogError($"Input path not found: {ArtworkInputPath}");
            return;
        }

        CopyArtworkIntoProject(ArtworkInputPath, ArtworkDestPath);

        // Step 2 — Refresh AssetDatabase so Unity sees the new files
        AssetDatabase.Refresh();

        // Step 3 — Mark each subfolder as its own Addressable group
        var subfolders = Directory.GetDirectories(ArtworkDestPath);
        int marked = 0;

        foreach (var folder in subfolders)
        {
            // Normalize to forward slashes for Unity's AssetDatabase
            string folderAssetPath = folder.Replace("\\", "/");
            string folderName = Path.GetFileName(folderAssetPath);

            var guids = AssetDatabase.FindAssets("", new[] { folderAssetPath });

            var validGuids = guids
                .Select(g => (guid: g, path: AssetDatabase.GUIDToAssetPath(g)))
                .Where(a => !AssetDatabase.IsValidFolder(a.path))
                .ToList();

            if (validGuids.Count == 0) continue;

            var group = settings.DefaultGroup;

            foreach (var (guid, assetPath) in validGuids)
            {
                try
                {
                    var entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
                    entry.address = $"Assets/Artwork/{folderName}/{Path.GetFileName(assetPath)}";
                    marked++;
                }
                catch
                {
                    Debug.LogError($"[AddressablesSetup] Could not process file: {assetPath}");
                }
            }

            Debug.Log($"[AddressablesSetup] Group '{folderName}': {validGuids.Count} assets marked.");
        }

        // Step 4 — Save settings
        settings.SetDirty(
            AddressableAssetSettings.ModificationEvent.EntryMoved,
            eventData: null,
            postEvent: true,
            settingsModified: true
        );
        AssetDatabase.SaveAssets();

        Debug.Log($"[AddressablesSetup] Done. {marked} assets marked across {subfolders.Length} groups.");
    }

    private static void CopyArtworkIntoProject(string sourcePath, string destPath)
    {
        // Ensure destination root exists
        Directory.CreateDirectory(destPath);

        foreach (var sourceFolder in Directory.GetDirectories(sourcePath))
        {
            string folderName = Path.GetFileName(sourceFolder);
            string destFolder = Path.Combine(destPath, folderName);
            Directory.CreateDirectory(destFolder);

            foreach (var file in Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories))
            {
                // Preserve subfolder structure within each ID folder
                string relativePath = file.Substring(sourceFolder.Length + 1);
                string destFile = Path.Combine(destFolder, relativePath);

                Directory.CreateDirectory(Path.GetDirectoryName(destFile));

                // Only copy if file is new or has been modified
                if (!File.Exists(destFile) ||
                    File.GetLastWriteTimeUtc(file) > File.GetLastWriteTimeUtc(destFile))
                {
                    try
                    {
                        File.Copy(file, destFile, overwrite: true);
                    } catch {
                        Debug.LogError("[AddressablesSetup] FAILED TO COPY FILE AT " + file);
                    }
                }
            }
        }

        Debug.Log($"[AddressablesSetup] Copied assets from {sourcePath} to {destPath}");
    }
}