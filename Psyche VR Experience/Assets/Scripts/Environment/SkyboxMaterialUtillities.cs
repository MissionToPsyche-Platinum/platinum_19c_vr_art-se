using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


// attached to Environment manager will allow it to auto set the skybox
// It will (re)assign the six textures to the existing "Skybox/6 Sided"
// material and set it as the scene skybox. Works in Edit and Play Mode.
[ExecuteAlways]
public class SkyboxMaterialUtilities : MonoBehaviour
{
    [Header("Asset Paths (Project-relative)")]
    [Tooltip("Project path to the existing material (e.g., Assets/Visual_assets/.../DiverseSpaceMaterial.mat)")]
    public string materialPath =  "Assets/Visual_assets/DeepSpaceSkyboxPack/DiverseSpace/Material/DiverseSpaceMaterial.mat";

    [Tooltip("Folder that contains the six textures (e.g., Assets/Visual_assets/.../Textures)")]
    public string texturesFolder = "Assets/Visual_assets/DeepSpaceSkyboxPack/DiverseSpace/Textures";

    [Header("Texture Base Name (no side, no extension)")]
    [Tooltip("Base name used by each face file, e.g., 'diversespace-' -> diversespace-front.png, diversespace-back.png, etc.")]
    public string baseName = "diversespace-";

    [Header("Behavior")]
    public bool applyOnEnable = true;
    public bool autoApplyOnValidate = true;

    // shader property IDs for Skybox/6 Sided
    static readonly int _FrontTex = Shader.PropertyToID("_FrontTex");
    static readonly int _BackTex = Shader.PropertyToID("_BackTex");
    static readonly int _LeftTex = Shader.PropertyToID("_LeftTex");
    static readonly int _RightTex = Shader.PropertyToID("_RightTex");
    static readonly int _UpTex = Shader.PropertyToID("_UpTex");
    static readonly int _DownTex = Shader.PropertyToID("_DownTex");

    void OnEnable()
    {
        if (applyOnEnable) ApplyTextures();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (autoApplyOnValidate && !EditorApplication.isCompiling)
            ApplyTextures();
    }
#endif

    [ContextMenu("Apply Now")]
    public void ApplyTextures()
    {
        Material material = LoadMaterial(materialPath);
        if (material == null)
        {
            Debug.LogError($"[{nameof(SkyboxMaterialUtilities)}] Could not load material at: {materialPath}", this);
            return;
        }

        if (material.shader == null || material.shader.name != "Skybox/6 Sided")
        {
            Debug.LogWarning($"[{nameof(SkyboxMaterialUtilities)}] Material is not using 'Skybox/6 Sided' (currently '{material.shader?.name ?? "null"}'). " +
                             $"I’ll still try assigning textures to the expected properties.", this);
        }

        // load textures by side
        var front = LoadTextureBySide("front");
        var back = LoadTextureBySide("back");
        var left = LoadTextureBySide("left");
        var right = LoadTextureBySide("right");
        var up = LoadTextureBySide("up");
        var down = LoadTextureBySide("down");

        // report missing ones (but still assign the rest)
        if (!front || !back || !left || !right || !up || !down)
        {
            Debug.LogWarning(
                $"[{nameof(SkyboxMaterialUtilities)}] Missing textures – " +
                $"Front:{(front ? "OK" : "null")}, Back:{(back ? "OK" : "null")}, Left:{(left ? "OK" : "null")}, Right:{(right ? "OK" : "null")}, Up:{(up ? "OK" : "null")}, Down:{(down ? "OK" : "null")}", this);
        }

        // assign textures to material (only set non-null so we don’t wipe good slots)
        if (front) material.SetTexture(_FrontTex, front);
        if (back) material.SetTexture(_BackTex, back);
        if (left) material.SetTexture(_LeftTex, left);
        if (right) material.SetTexture(_RightTex, right);
        if (up) material.SetTexture(_UpTex, up);
        if (down) material.SetTexture(_DownTex, down);

        // assign material as skybox
        RenderSettings.skybox = material;
        DynamicGI.UpdateEnvironment();

#if UNITY_EDITOR
        EditorUtility.SetDirty(material);
        EditorUtility.SetDirty(this);
#endif
    }

    private Texture2D LoadTextureBySide(string sideLower)
    {
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(texturesFolder) || string.IsNullOrEmpty(baseName))
            return null;

        // search by name inside the folder: e.g., "diversespace-front t:Texture2D"
        string nameFilter = $"{baseName}{sideLower}";
        string[] guids = AssetDatabase.FindAssets($"{nameFilter} t:Texture2D", new[] { texturesFolder });
        if (guids != null && guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // try the exact path with common extensions in case of failure
        string[] extensions = { ".png", ".jpg", ".jpeg", ".tga", ".tif", ".tiff", ".psd", ".exr", ".hdr" };
        foreach (var ext in extensions)
        {
            string p = $"{texturesFolder}/{baseName}{sideLower}{ext}";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
            if (texture) return texture;
        }
#endif
        return null;
    }

    private Material LoadMaterial(string path)
    {
#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(path))
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat) return mat;
        }
#endif
        // in builds (no AssetDatabase): rely on whatever is already assigned in RenderSettings
        return RenderSettings.skybox;
    }
}
