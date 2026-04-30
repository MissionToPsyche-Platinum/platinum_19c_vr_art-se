using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;

public static class BuildScript
{
    public static void BuildAddressables()
    {
        if(EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            bool switch_target = EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            if (!switch_target)
            {
                throw new System.Exception("TARGET SWITCH FAILED!!!");
            }
        }


        AddressablesSetup.MarkArtworkAddressable();

        AddressableAssetSettings.BuildPlayerContent();

    }
}
