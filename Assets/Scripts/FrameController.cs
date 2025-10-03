using System.Collections.Generic;
using UnityEngine;

public class FrameController : MonoBehaviour
{
    public enum ScaleMode { LongerSide, PixelArea }

    [Header("Scene References")]
    [Tooltip("The Quad that shows the image (uses MeshRenderer with a material).")]
    [SerializeField] MeshRenderer imageQuadRenderer;

    [Tooltip("Material for the frame borders (4 extruded squares to make up edges).")]
    [SerializeField] Material frameMaterial;

    [Header("Images")]
    [Tooltip("All images this frame can show. Only one is visible at a time. API is available.")]
    [SerializeField] List<Texture2D> images = new List<Texture2D>();

    [Tooltip("Index into Images to show.")]
    [SerializeField] int currentImageIndex = 0;

    [Header("Aspect & Sizing")]

    [Tooltip("How overall frame scale is derived from resolution vs Base Resolution.")]
    [SerializeField] ScaleMode scaleMode = ScaleMode.LongerSide;

    [Tooltip("Base resolution used for scaling. Ex: 1920x1080.")]
    [SerializeField] Vector2Int baseResolution = new Vector2Int(1920, 1080);

    [Tooltip("Nominal image height in local units before resolution scaling (the script scales from here).")]
    [SerializeField] float nominalImageHeight = 1.0f;

    [Header("Frame Geometry")]
    [Tooltip("Frame border thickness around visible image (in local units).")]
    [SerializeField] float borderThickness = 0.05f;

    [Tooltip("Frame depth (how far the border extends along the relative z-axis(front to back)).")]
    [SerializeField] float frameDepth = 0.05f;

    [Header("Scriptable Object(Artwork)")]
    [Tooltip("The scriptable object containing the piece's path and data. (This is not included in functionality nor in the API atm)")]
    [SerializeField] Object scriptable = null;

    // names for borders
    const string BORDER_PARENT = "Borders";
    const string TOP = "Top";
    const string BOTTOM = "Bottom";
    const string LEFT = "Left";
    const string RIGHT = "Right";

    // holds the empty object for us
    Transform bordersParent;

    void OnValidate()
    {
        borderThickness = Mathf.Max(0.0001f, borderThickness);
        frameDepth = Mathf.Max(0.0f, frameDepth);
        nominalImageHeight = Mathf.Max(0.001f, nominalImageHeight);
        currentImageIndex = Mathf.Clamp(currentImageIndex, 0, Mathf.Max(0, images.Count - 1));
        ApplyAll();
    }

    void Reset()
    {
        // auto-locate the image quad(surface for the image)
        if (!imageQuadRenderer)
        {
            var quad = transform.Find("ImageQuad");
            if (quad) imageQuadRenderer = quad.GetComponent<MeshRenderer>();
        }
        ApplyAll();
    }

    void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            ApplyAll();
        }
#endif
    }

    /*  PUBLIC API  */

    public void SetImageIndex(int index)
    {
        if (images == null || images.Count == 0) return;
        currentImageIndex = Mathf.Clamp(index, 0, images.Count - 1);
        ApplyAll();
    }

    public void NextImage()
    {
        if (images == null || images.Count == 0) return;
        currentImageIndex = (currentImageIndex + 1) % images.Count;
        ApplyAll();
    }

    public void PreviousImage()
    {
        if (images == null || images.Count == 0) return;
        currentImageIndex = (currentImageIndex - 1 + images.Count) % images.Count;
        ApplyAll();
    }

    /* @TODO 10/2/25 zkbarlow
     * 
     * I would like to add auto-scrolling for some sort of play button.
     * Allows the viewer to push one button and then watch the images 
     * change one by one.*/


    /*  CORE FUNCTIONALITY  */

    void ApplyAll()
    {
        if (!imageQuadRenderer) return;

        var tex = (images != null && images.Count > 0) ? images[Mathf.Clamp(currentImageIndex, 0, images.Count - 1)] : null;

        // apply the texture to the quad material
        var mat = imageQuadRenderer.sharedMaterial;
        if (mat == null)
        {
            // failsafe if no default material(unity forum told me to)
#if UNITY_EDITOR
            var shader = Shader.Find("Sprite-Unlit-Default");
            if (shader)
            {
                mat = new Material(shader);
                imageQuadRenderer.sharedMaterial = mat;
            }
#endif
        }
        if (mat && tex) mat.mainTexture = tex;

        // compute the aspect ratio and then set image quad local scale
        Vector2Int resolution = tex ? new Vector2Int(tex.width, tex.height) : baseResolution;
        float aspectRatio = resolution.x / (float)resolution.y; // width / height
        float imgHeight = nominalImageHeight;
        float imgWidth = imgHeight * aspectRatio;

        Transform quadT = imageQuadRenderer.transform;
        quadT.localScale = new Vector3(imgWidth, imgHeight, 1f);

        // builds/updates border geometry
        EnsureBorders();
        UpdateBorders(imgWidth, imgHeight);

        // scale entire frame based on resolution vs base(reference) resolution
        float overallScale = ComputeResolutionScale(resolution, baseResolution, scaleMode);
        transform.localScale = new Vector3(overallScale, overallScale, overallScale);
    }

    float ComputeResolutionScale(Vector2Int resolution, Vector2Int baseResolution, ScaleMode scaleMode)
    {
        // longer-side ratio or area ratio (sqrt of pixel area ratio)
        switch (scaleMode)
        {
            case ScaleMode.PixelArea:
                float area = (float)resolution.x * resolution.y;
                float baseArea = (float)baseResolution.x * baseResolution.y;
                return Mathf.Sqrt(Mathf.Max(0.000001f, area / baseArea));
            case ScaleMode.LongerSide:
            default:
                float maxSide = Mathf.Max(resolution.x, resolution.y);
                float baseMaxSide = Mathf.Max(baseResolution.x, baseResolution.y);
                return Mathf.Max(0.000001f, maxSide / baseMaxSide);
        }
    }

    void EnsureBorders()
    {
        // checks for borders parent object, if none, will create one
        if (!bordersParent)
        {
            var foundBorderParent = transform.Find(BORDER_PARENT);
            if (foundBorderParent) bordersParent = foundBorderParent;
        }
        if (!bordersParent)
        {
            var gameObject = new GameObject(BORDER_PARENT);
            bordersParent = gameObject.transform;
            bordersParent.SetParent(transform, false);
        }

        EnsureEdge(TOP);
        EnsureEdge(BOTTOM);
        EnsureEdge(LEFT);
        EnsureEdge(RIGHT);
    }

    void EnsureEdge(string parentName)
    {
        var transformParent = bordersParent.Find(parentName);
        if (!transformParent)
        {
            var gameObjectEdge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gameObjectEdge.name = parentName;
            var meshRenderer = gameObjectEdge.GetComponent<MeshRenderer>();
            var meshCollider = gameObjectEdge.GetComponent<MeshCollider>(); // remove collider to keep scene clean
            if (meshCollider) DestroyImmediate(meshCollider);
            if (frameMaterial && meshRenderer) meshRenderer.sharedMaterial = frameMaterial;
            transformParent = gameObjectEdge.transform;
            transformParent.SetParent(bordersParent, false);
        }
    }

    /*
     * Updates the borders based on image height and width
     */
    void UpdateBorders(float imgWidth, float imgHeight)
    {
        // inner “window” is the image plane edges; frame sits around it with borderThickness and a bit of depth.
        // 4 strips to be used as frame edges
        var topEdge = bordersParent.Find(TOP);
        var bottomEdge = bordersParent.Find(BOTTOM);
        var leftEdge = bordersParent.Find(LEFT);
        var rightEdge = bordersParent.Find(RIGHT);

        float thickness = borderThickness;
        float depth = frameDepth;


        // horizontal edges (top/bottom): width spans image width + 2*borderThickness, thickness is borderThickness
        float outerWidth = imgWidth + 2f * thickness;
        float edgeH = thickness;
        float edgeZ = depth;

        if (topEdge)
        {
            topEdge.localPosition = new Vector3(0f, (imgHeight * 0.5f) + (edgeH * 0.5f), 0f);
            topEdge.localScale = new Vector3(outerWidth, edgeH, edgeZ);
        }
        if (bottomEdge)
        {
            bottomEdge.localPosition = new Vector3(0f, -(imgHeight * 0.5f) - (edgeH * 0.5f), 0f);
            bottomEdge.localScale = new Vector3(outerWidth, edgeH, edgeZ);
        }

        // vertical edges (left/right): height spans image height, thickness is borderThickness
        float edgeW = thickness;
        float outerH = imgHeight + 2f * thickness;

        if (leftEdge)
        {
            leftEdge.localPosition = new Vector3(-(imgWidth * 0.5f) - (edgeW * 0.5f), 0f, 0f);
            leftEdge.localScale = new Vector3(edgeW, outerH, edgeZ);
        }
        if (rightEdge)
        {
            rightEdge.localPosition = new Vector3((imgWidth * 0.5f) + (edgeW * 0.5f), 0f, 0f);
            rightEdge.localScale = new Vector3(edgeW, outerH, edgeZ);
        }

        // puts ImageQuad slightly behind the frame face so borders are visible as raised edges(makes the borders look like a frame)
        if (imageQuadRenderer)
        {
            var qt = imageQuadRenderer.transform;
            qt.localPosition = new Vector3(0f, 0f, -0.001f);
        }
    }
}
