using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

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
    [SerializeField] private List<string> mediaPaths = new List<string>();

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

    // per-renderer property block for per-instance textures
    MaterialPropertyBlock _mpb;

    // Auto Iterate stuff.
    private Coroutine autoIterateRoutine;
    [SerializeField, Tooltip("Seconds between automatic image switches when auto-iteration is running.")]
    private float autoIterationInterval = 5f;

    // video playback stuff
    private VideoPlayer videoPlayer; 
    private RenderTexture videoTexture;
    private bool isVideoMode = false;


    void Awake()
    {
        if (_mpb == null) _mpb = new MaterialPropertyBlock();
    }

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
    public void SetArtwork(ArtworkData data)
    {
        if (data == null) return;

        // store the paths for potential video playback as well
        mediaPaths = new List<string>(data.artworkURLs);

        List<Texture2D> textures = data.LoadTextures();
        if (textures.Count > 0)
        {
            images = textures; 
            currentImageIndex = 0;
            ApplyAll();
        }

        //rotate the image quad 180(images are all backwards)
        if (imageQuadRenderer != null)
        {
            imageQuadRenderer.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        }
        // @TODO assign UI text fields
    }

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

    /*  CORE FUNCTIONALITY  */
    void ApplyAll()
    {
        if (!imageQuadRenderer) return;

        var tex = (images != null && images.Count > 0) ? images[Mathf.Clamp(currentImageIndex, 0, images.Count - 1)] : null;


        // ***** video handling portion of apply all *****

        if (scriptable is ArtworkData artData &&
            artData.artworkURLs != null &&
            currentImageIndex < artData.artworkURLs.Count)
        {
            string relPath = artData.artworkURLs[currentImageIndex];

            // Convert "Assets/..." to absolute file path
            string fullPath = System.IO.Path.Combine(
                Application.dataPath,
                relPath.Substring("Assets/".Length)
            );

            // if its a video, switch to the video playback functionality.
            // stops previous video playback(if applicable!!!does not wait for video completion, this is just to make sure videos play)
            if (IsVideoFile(fullPath))
            {
                StopVideoIfNeeded();   // stop previously playing video if applicable
                PlayVideo(fullPath);   // play new one
                return;                
            }
        }

        // if we reach this point, ensure no lingering video playback or ghost frames(spooky!)
        StopVideoIfNeeded();

        // set texture via MaterialPropertyBlock (per-renderer), not sharedMaterial as it was
        imageQuadRenderer.GetPropertyBlock(_mpb);

        // Try both common property names to be robust across shaders (URP Lit vs legacy)
        if (tex != null)
        {
            _mpb.SetTexture("_BaseMap", tex);
            _mpb.SetTexture("_MainTex", tex);
        }
        else
        {
            // clear if no texture
            _mpb.SetTexture("_BaseMap", null);
            _mpb.SetTexture("_MainTex", null);
        }

        imageQuadRenderer.SetPropertyBlock(_mpb);

        // compute the aspect ratio and then set image quad local scale
        Vector2Int resolution = tex ? new(tex.width, tex.height) : baseResolution;
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

    void UpdateBorders(float imgWidth, float imgHeight)
    {
        var topEdge = bordersParent.Find(TOP);
        var bottomEdge = bordersParent.Find(BOTTOM);
        var leftEdge = bordersParent.Find(LEFT);
        var rightEdge = bordersParent.Find(RIGHT);

        float thickness = borderThickness;
        float depth = frameDepth;

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

        if (imageQuadRenderer)
        {
            var qt = imageQuadRenderer.transform;
            qt.localPosition = new Vector3(0f, 0f, -0.001f);
        }
    }

    // safe getter for media paths
    public string GetMediaPath(int index)
    {
        if (mediaPaths == null || index < 0 || index >= mediaPaths.Count)
            return null;
        return mediaPaths[index];
    }

    /// Starts automatic image cycling for this frame.
    /// <param name="intervalSeconds">time in seconds between each image switch.</param>
    public void StartAutoIteration(float intervalSeconds = -1f)
    {
        // will update the variable, otherwise 5
        if (intervalSeconds > 0f)
            autoIterationInterval = intervalSeconds;

        StopAutoIteration(); // ensure no duplicate coroutines
        autoIterateRoutine = StartCoroutine(AutoIterateCoroutine());
    }

    // stops auto-iteration if it’s currently active.
    public void StopAutoIteration()
    {
        if (autoIterateRoutine != null)
        {
            StopCoroutine(autoIterateRoutine);
            autoIterateRoutine = null;
        }
    }

    // toggles for auto iteration 
    public void ToggleAutoIteration(float intervalSeconds = -1f)
    {
        if (autoIterateRoutine != null)
        {
            StopAutoIteration();
        }
        else
        {
            StartAutoIteration(intervalSeconds);
        }
    }

    // coroutine instance
    // (only handles images rn. Need to add video functionality before I add waiting for videos to finish)
    private IEnumerator AutoIterateCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(autoIterationInterval);
            NextImage();
        }
    }

    // ***** Video and Audio Handling *****

    // helper for detecting videofile(just checks the extension)
    private bool IsVideoFile(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;
        string ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
        return ext == ".mp4" || ext == ".mov" || ext == ".m4v" || ext == ".avi" || ext == ".webm";
    }

    //
    private void OnVideoPrepared(VideoPlayer source)
    {
        source.prepareCompleted -= OnVideoPrepared;

        // start video playback
        source.Play();

        // apply the RenderTexture to the quad
        imageQuadRenderer.GetPropertyBlock(_mpb);
        _mpb.SetTexture("_BaseMap", videoTexture);
        _mpb.SetTexture("_MainTex", videoTexture);
        imageQuadRenderer.SetPropertyBlock(_mpb);
    }

    private void StopVideoIfNeeded()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }

        isVideoMode = false;
    }



    private void PlayVideo(string fullPath)
    {
        if (!System.IO.File.Exists(fullPath))
        {
            Debug.LogWarning("[FrameController]Video file not found: " + fullPath);
            return;
        }

        // creates new or configures current video player component
        if (videoPlayer == null)
        {
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
            videoPlayer.playOnAwake = false;

            // render to RenderTexture
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.isLooping = false;
            videoPlayer.waitForFirstFrame = true;
            
            // callback assigns texture
            videoPlayer.prepareCompleted += OnVideoPrepared;
        }

        // create the renderTexture (lazy, doesn't check the video aspect ratio.)
        // @TODO add video scaling detection here
        if (videoTexture == null)
        {
            videoTexture = new RenderTexture(1920, 1080, 0);
        }
        videoPlayer.targetTexture = videoTexture;

        // assigns the media path to the video player
        videoPlayer.url = fullPath;
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.Prepare();
        isVideoMode = true;
    }

}
