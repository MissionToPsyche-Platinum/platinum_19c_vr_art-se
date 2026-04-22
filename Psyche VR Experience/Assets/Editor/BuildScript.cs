using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;

public static class BuildScript
{
    public static void BuildAddressables()
    {
        AddressablesSetup.MarkArtworkAddressable();

        AddressableAssetSettings.BuildPlayerContent();

    }
}
