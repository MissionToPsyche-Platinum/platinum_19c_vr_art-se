using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Purchasing;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.Video;
using UnityEngine.ResourceManagement.AsyncOperations;

public class FrameController : MonoBehaviour
{
    [Header("Transform of Image Frame to Resize")]
    public Transform frame;

    public enum ScaleMode { LongerSide, PixelArea }

    [Header("Scene References")]
    [Tooltip("The Quad that shows the image (uses MeshRenderer with a material).")]
    [SerializeField] MeshRenderer imageQuadRenderer;

    [Tooltip("Material for the frame borders (4 extruded squares to make up edges).")]
    [SerializeField] Material frameMaterial;

    [Header("Images")]
    [SerializeField] private List<string> mediaPaths = new List<string>();
    private Texture2D lastLoadedImage = null; // for use in unloading previous texture from memory

    [Tooltip("Index into Images to show.")]
    [SerializeField] int currentMediaIndex = 0;

    [Header("Aspect & Sizing")]
    [Tooltip("How overall frame scale is derived from resolution vs Base Resolution.")]
    [SerializeField] ScaleMode scaleMode = ScaleMode.LongerSide;

    [Tooltip("Base resolution used for scaling. Ex: 1920x1080.")]
    [SerializeField] Vector2Int baseResolution = new Vector2Int(1920, 1080);

    [Tooltip("Nominal image height in local units before resolution scaling (the script scales from here).")]
    [SerializeField] float nominalImageHeight = 5f;

    [Header("World-Space Frame Size Clamp")]
    [SerializeField] private bool clampWorldSize = true;

    [SerializeField, Tooltip("Max world-space height (units?) for the image quad.")]
    private float maxWorldHeight = 8.0f; // this is a rough estimate

    [SerializeField, Tooltip("Max world-space width (units?). Set <= 0 to ignore.")]
    private float maxWorldWidth = 6.5f; // this is a rough estimate

    [Header("Wall Placement")]
    [SerializeField] private bool clampToWallBand = false;
    [SerializeField, Range(0f, 0.5f), Tooltip("The percentage of the bottom of the wall reserved for text")] 
    private float reservedBottomPercent = 0.2f; // bottom x% reserved for text
    [SerializeField] private float targetCenterWorldY = 1.6f;   // <-- you set this
    [SerializeField] private float extraBottomPadding = 0.05f;                   // small safety buffer

    [Header("Frame Geometry")]
    [Tooltip("Frame border thickness around visible image (in local units).")]
    [SerializeField] float borderThickness = 0.05f;

    [Tooltip("Frame depth (how far the border extends along the relative z-axis(front to back)).")]
    [SerializeField] float frameDepth = 0.05f;

    [Header("Fallback Texture")]
    [Tooltip("An image to display if the frame has nothing to display within itself due to errors or the media being audio only")]
    [SerializeField] Texture2D fallbackTexture;


    // names for borders
    const string BORDER_PARENT = "Borders";
    const string TOP = "Top";
    const string BOTTOM = "Bottom";
    const string LEFT = "Left";
    const string RIGHT = "Right";

    // holds the empty object for us
    Transform bordersParent;
    private Renderer wallRendererRef;

    // per-renderer property block for per-instance textures
    MaterialPropertyBlock _mpb;


    // video/audio playback stuff
    private VideoPlayer videoPlayer; 
    private RenderTexture videoTexture;
    private bool isVideoMode = false;
    private double currentVideoDuration = 0.0;  // video duration for auto-iteration
    private AudioSource audioSource;
    [SerializeField] private bool enableAudio = true; //optional toggle 
    [Header("Video Playback")]
    [SerializeField] private bool autoPlayVideoOnLoad = false;

    [Header("Video Proximity Autoplay")]
    [SerializeField] private bool playVideoOnPlayerProximity = true;
    [SerializeField] private float videoDwellSeconds = 0.75f;
    [SerializeField] private bool pauseOnExit = true;
    // tags that count as the player, I just used "Player" and assigned XR origin, freeroam camera, and playercamera as "Player"
    [SerializeField] private string collisionTag = "MainCamera";
    private GameObject pauseSymbol = null;
    private GameObject pauseButton = null;
    private int insideCount = 0;
    private Coroutine dwellRoutine;

    [Header("Component References")]
    [SerializeField] private TextBoxController textBoxController;
    [SerializeField] private GameObject buttonNext;
    [SerializeField] private GameObject buttonPrev;

    //[HideInInspector]
    public AsyncOperationHandle handle = default;
    public bool mediaLoaded = false;
    bool applyAllRunning = false;

    void Awake()
    {
        SettingsManager.m_VideoVolumeChanged.AddListener(VolumeChanged);

        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        pauseSymbol = this.gameObject.transform.Find("ImageQuad").transform.Find("PauseSymbol").gameObject;
        pauseButton = this.gameObject.transform.Find("Buttons").transform.Find("VRButton_Pause").gameObject;

        SettingsManager.m_ButtonSizeChanged.AddListener(RepositionButtons_Callback);
    }

    void OnValidate()
    {
        borderThickness = Mathf.Max(0.0001f, borderThickness);
        frameDepth = Mathf.Max(0.0f, frameDepth);
        nominalImageHeight = Mathf.Max(0.001f, nominalImageHeight);
        currentMediaIndex = Mathf.Clamp(currentMediaIndex, 0, Mathf.Max(0, mediaPaths.Count - 1));
        //ApplyAll();
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

    private void OnDestroy()
    {
        SettingsManager.m_VideoVolumeChanged.RemoveListener(VolumeChanged);
        SettingsManager.m_VideoVolumeChanged.RemoveListener(RepositionButtons_Callback);

        if (handle.IsValid())
            Addressables.Release(handle);
    }

    public bool apply_all_manual = false;

    void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            ApplyAll();
        }
        if (apply_all_manual)
        {
            ApplyAll();
            apply_all_manual = false;
        }

#endif
    }

    /*  PUBLIC API  */
    public void SetArtwork(ArtworkData data)
    {
        if (data.artworkCount == 0)
        {
            return;
        }

        // store the paths for potential video playback as well
        // while filtering out missing files(in case db got some bad entries)
        //var validated = new List<string>();
        //foreach (string relPath in data.artworkURLs)
        //{
        //    string full = ResolveFullPath(relPath);
        //    if (!string.IsNullOrEmpty(full) && System.IO.File.Exists(full))
        //    {
        //        validated.Add(relPath);
        //    }
        //    else
        //    {
        //        Debug.LogWarning($"[FrameController] Removing missing media file from list: {relPath}");
        //    }
        //}
        mediaPaths = data.artworkURLs;

        currentMediaIndex = 0;
        ApplyAll();     // will now decide image vs video properly


        //rotate the image quad 180(images are all backwards)
        if (imageQuadRenderer != null)
        {
            imageQuadRenderer.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        }

        // @TODO assign UI text fields

        if (mediaPaths.Count > 1)
        {
            //set buttons active if there's more than one art piece
            buttonNext.SetActive(true);
            buttonPrev.SetActive(true);
        } 
        else
        {
            buttonNext.SetActive(false);
            buttonPrev.SetActive(false);
        }

        textBoxController.SetDescText(data);
    }
    
    public async void RepositionButtons_Callback()
    {
        //this is just to wait a frame for buttons to be resized
        await Task.Delay(1);

        RepositionButtons();
    }

    public void RepositionButtons()
    {
        //this calculation assumes that both buttons are the same size
        float dist = Mathf.Clamp(buttonNext.transform.lossyScale.x / 2 + frame.localScale.x * 0.5f, 0.25f, 99f);

        Vector3 posNext = new Vector3(-dist, 0, 0);
        Vector3 posPrev = new Vector3(dist, 0, 0);

        buttonNext.transform.localPosition = posNext;
        buttonPrev.transform.localPosition = posPrev;
    }

    public void SetImageIndex(int index)
    {
        if (mediaPaths == null || mediaPaths.Count == 0) return;
        currentMediaIndex = Mathf.Clamp(index, 0, mediaPaths.Count - 1);
        ApplyAll();
    }

    public void ButtonNext()
    {
        if(mediaPaths == null || mediaPaths.Count == 0) {

            buttonNext.SetActive(false);
            buttonPrev.SetActive(false);
            return; 
        }

        NextImage();

        //this is necessary to ensure that auto iteration doesn't
        // get in the way of button iteration, and if a user
        // is manually iterating, we just want to turn this off
    }

    public void ButtonPrevious()
    {
        if (mediaPaths == null || mediaPaths.Count == 0)
        {
            buttonNext.SetActive(false);
            buttonPrev.SetActive(false);
            return;
        }

        PreviousImage();

        //this is necessary to ensure that auto iteration doesn't
        // get in the way of button iteration, and if a user
        // is manually iterating, we just want to turn this off
    }

    public async void NextImage()
    {
        if (mediaPaths == null || mediaPaths.Count == 0) return;
        currentMediaIndex = (currentMediaIndex + 1) % mediaPaths.Count;
        await ApplyAll();
        textBoxController.UpdateTextLocation();
    }

    public async void PreviousImage()
    {
        if (mediaPaths == null || mediaPaths.Count == 0) return;
        currentMediaIndex = (currentMediaIndex - 1 + mediaPaths.Count) % mediaPaths.Count;
        await ApplyAll();
        textBoxController.UpdateTextLocation();
    }

    async Task ApplyAll()
    {
        while (applyAllRunning)
        {
            await Task.Delay(100);
        }

        applyAllRunning = true;

        mediaLoaded = false;

        if (!imageQuadRenderer) return;
        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        // Ensure valid index or display generic psyche logo instead
        if (mediaPaths == null || mediaPaths.Count == 0)
        {
            Debug.LogWarning("[FrameController] No media found");
            return;
        }

        if (currentMediaIndex < 0 || currentMediaIndex >= mediaPaths.Count)
            currentMediaIndex = 0;

        string key = mediaPaths[currentMediaIndex];

        try
        {
            AsyncOperationHandle<UnityEngine.Object> handle = Addressables.LoadAssetAsync<UnityEngine.Object>(key);

            if (this.handle.IsValid())
            {
                Addressables.Release(this.handle);
            }
            
            this.handle = handle;
            //LaunchRoomManager.handles.Add(handle);

            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                UnityEngine.Object asset = handle.Result;

                if (asset is AudioClip)
                {
                    AudioClip clip = (AudioClip)asset;

                    //Addressables.Release(handle);

                    if (clip != null)
                    {
                        NextImage();
                        applyAllRunning = false;
                        return;
                    }
                }
                else if (asset is VideoClip)
                {
                    VideoClip clip = (VideoClip)asset;

                    //Addressables.Release(handle);

                    if (clip != null)
                    {
                        ShowVideo(clip);
                        applyAllRunning = false;
                        mediaLoaded = true;
                        return;
                    }
                }
                else if (asset is Texture2D)
                {

                    Texture2D tex = (Texture2D)asset;

                    StopVideoIfNeeded();
                    currentVideoDuration = 0.0;

                    // destroy previous texture, if any
                    if (lastLoadedImage != null)
                    {
                        //DestroyImmediate(lastLoadedImage, true);
                        lastLoadedImage = null;
                    }

                    //LaunchRoomManager.handles.Add(handle);

                    imageQuadRenderer.GetPropertyBlock(_mpb);
                    _mpb.SetTexture("_BaseMap", tex);
                    _mpb.SetTexture("_MainTex", tex);
                    imageQuadRenderer.SetPropertyBlock(_mpb);

                    // Sizing based on the current texture
                    Vector2Int res = tex ? new(tex.width, tex.height) : baseResolution;
                    ResizeFrame(res);
                }
                else
                {
                    Addressables.Release(handle);
                    Debug.LogError($"ASSET UNRECOGNIZED! TRIED TO LOAD ASSET AT KEY {key}");
                    applyAllRunning = false;
                    return;
                }
            } else
            {
                Addressables.Release(handle);
                Debug.LogError($"ASSET LOAD FAILED!");
                mediaLoaded = false;
                applyAllRunning = false;
                return;
            }

            mediaLoaded = true;
            applyAllRunning = false;

        }
        catch (Exception e)
        {
            Debug.LogError(e);
            applyAllRunning = false;
            return;
        }
    }

    /* --------------------------------------------------------------
     * PATH RESOLUTION + SAFE IMAGE LOADING
     * -------------------------------------------------------------- */


    /* --------------------------------------------------------------
     * FRAME SCALE AND RESOLUTION
     * -------------------------------------------------------------- */

    public void SetWorldSizeClamp(float maxH, float maxW)
    {
        clampWorldSize = (maxH > 0f || maxW > 0f);
        maxWorldHeight = Mathf.Max(0.01f, maxH);
        maxWorldWidth = Mathf.Max(0.01f, maxW);
    }

    public void ConfigureWallPlacement(Renderer wallRenderer, float targetY, float reservedBottom = 0.2f, float padding = 0.05f)
    {
        wallRendererRef = wallRenderer;
        clampToWallBand = (wallRendererRef != null);
        targetCenterWorldY = targetY;
        reservedBottomPercent = Mathf.Clamp(reservedBottom, 0f, 0.5f);
        extraBottomPadding = Mathf.Max(0f, padding);
    }

    private void ResizeFrame(Vector2Int resolution)
    {
        float aspect = resolution.x / (float)resolution.y;

        // quad size BEFORE clamp
        float height = nominalImageHeight;
        float width = height * aspect;

        // scale the quad
        Transform quadT = imageQuadRenderer.transform;
        quadT.localScale = new Vector3(width, height, 1f);

        // rebuild borders
        //EnsureBorders();
        //UpdateBorders(width, height);

        float overall = ComputeResolutionScale(resolution, baseResolution, scaleMode);  // global scale

        frame.transform.localScale = new Vector3(overall, overall, overall);

        if (clampWorldSize)
        {
            // current world size of the displayed quad
            float worldHeight = imageQuadRenderer.bounds.size.y;
            float worldWidth = imageQuadRenderer.bounds.size.x;

            // ratios needed to fit within limits
            // NOTE: this will only ever shrink a frame
            float hRatio = (worldHeight > maxWorldHeight) ? (maxWorldHeight / worldHeight) : 1f;
            float wRatio = (worldWidth > maxWorldWidth) ? (maxWorldWidth / worldWidth) : 1f;

            float ratio = Mathf.Min(hRatio, wRatio);
            // takes the bound that exceeds its param by the most then scales it
            // down at the correct ratio
            if (ratio < 1f)
                frame.transform.localScale *= ratio;
        }
        ApplyWallClampToTargetY();

        //after frame is resized, reposition the buttons
        RepositionButtons();
    }

    // the point here is to clamp the frame to a specific height if it can go there 
    // while also leaving room for the text to display below it. The height it is
    // can be adjusted, it's in the room module script.
    private void ApplyWallClampToTargetY()
    {
        if (!clampToWallBand || wallRendererRef == null) return;
        if (imageQuadRenderer == null) return;

        Bounds wallB = wallRendererRef.bounds;
        Bounds frameB = imageQuadRenderer.bounds;

        float wallMinY = wallB.min.y;
        float wallMaxY = wallB.max.y;

        float reservedBottomY = wallMinY + (wallB.size.y * reservedBottomPercent);

        float halfH = frameB.extents.y;

        float minCenterY = reservedBottomY + extraBottomPadding + halfH;
        float maxCenterY = wallMaxY - halfH;

        float clampedY = Mathf.Clamp(targetCenterWorldY, minCenterY, maxCenterY);

        var p = transform.position;
        transform.position = new Vector3(p.x, clampedY, p.z);
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

    /* --------------------------------------------------------------
    *                   VIDEO AND AUDIO HANDLING
    * -------------------------------------------------------------- */

    private void OnVideoPrepared(VideoPlayer vp)
    {
        vp.prepareCompleted -= OnVideoPrepared;

        int w = (int)vp.width;
        int h = (int)vp.height;

        // Correct RenderTexture size
        if (videoTexture == null || videoTexture.width != w || videoTexture.height != h)
        {
            if (videoTexture != null) videoTexture.Release();
            videoTexture = new RenderTexture(w, h, 0);
        }

        vp.targetTexture = videoTexture;

        // Apply to quad
        imageQuadRenderer.GetPropertyBlock(_mpb);
        _mpb.SetTexture("_BaseMap", videoTexture);
        _mpb.SetTexture("_MainTex", videoTexture);
        imageQuadRenderer.SetPropertyBlock(_mpb);

        // store the real duration for auto-iteration
        currentVideoDuration = vp.length;

        // Show first frame only
        vp.Play();
        vp.Pause();
        vp.isLooping = vp.url.Contains("_GIF");
        pauseSymbol.SetActive(true);
        pauseButton.SetActive(true);

        if (enableAudio && audioSource != null)
            audioSource.Pause();

        // auto-play if explicitly enabled
        if (autoPlayVideoOnLoad)
        {
            PlayPreparedVideo();
        }

        ResizeFrame(new Vector2Int(w, h));
    }


    // will be used for proximity triggering
    private void PlayPreparedVideo()
    {
        if (videoPlayer == null) return;

        videoPlayer.Play();

        if (enableAudio && audioSource != null)
            audioSource.Play();
        pauseSymbol.SetActive(false);

    }

    // will be used for proximity triggering and for pausing videos.
    // Stop will reset video and audio time to zero so differing functionality is necessary
    private void PausePreparedVideo()
    {
        if (videoPlayer != null) videoPlayer.Pause();
        if (audioSource != null) audioSource.Pause();
        pauseSymbol.SetActive(true);
    }

    private void StopVideoIfNeeded()
    {
        if (videoPlayer != null)
            videoPlayer.Stop();

        if (audioSource != null)
            audioSource.Stop();

        isVideoMode = false;
    }

    private void ShowVideo(VideoClip clip)
    {
        // stop any currently running video
        StopVideoIfNeeded();

        // free last image texture if switching to video
        if (lastLoadedImage != null)
        {
            lastLoadedImage = null;
        }

        // start playing this video
        PlayVideo(clip);

        // mark that we are now in video mode
        isVideoMode = true;
    }

    private async void PlayVideo(string fullPath)
    {
        var locations = await Addressables.LoadResourceLocationsAsync(fullPath.Replace('\\', '/')).Task;
        string path = locations[0].PrimaryKey;

        if (!System.IO.File.Exists(path))
        {
            Debug.LogWarning("[FrameController] Video not found: " + path);
            return;
        }

        if (videoPlayer == null)
        {
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
            videoPlayer.playOnAwake = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.isLooping = false;
        }

        // Setup audio
        if (enableAudio)
        {
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f;                  // Make audio 3D
                audioSource.rolloffMode = AudioRolloffMode.Linear;
                audioSource.minDistance = .5f;                  // Start fading out
                audioSource.maxDistance = 3f;                   // ~silent by this distance
                audioSource.dopplerLevel = 0f;                  // Avoid doppler shift
                audioSource.loop = false;                       // no looping ever please
                audioSource.spread = 0f;                        // 0 = more directional  
                audioSource.volume = GlobalSettings.MASTER_VOLUME;
            }

            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.EnableAudioTrack(0, true);
            videoPlayer.SetTargetAudioSource(0, audioSource);
        }
        else
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        }

        // Assign video path
        videoPlayer.url = path;

        // Only register ONCE
        videoPlayer.prepareCompleted -= OnVideoPrepared;
        videoPlayer.prepareCompleted += OnVideoPrepared;

        videoPlayer.Prepare();
        isVideoMode = true;
    }

    private void PlayVideo(VideoClip clip)
    {
        if (videoPlayer == null)
        {
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
            videoPlayer.playOnAwake = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.isLooping = false;
        }

        // Setup audio
        if (enableAudio)
        {
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f;                  // Make audio 3D
                audioSource.rolloffMode = AudioRolloffMode.Linear;
                audioSource.minDistance = .5f;                  // Start fading out
                audioSource.maxDistance = 3f;                   // ~silent by this distance
                audioSource.dopplerLevel = 0f;                  // Avoid doppler shift
                audioSource.loop = false;                       // no looping ever please
                audioSource.spread = 0f;                        // 0 = more directional  
                audioSource.volume = GlobalSettings.MASTER_VOLUME;
            }

            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.EnableAudioTrack(0, true);
            videoPlayer.SetTargetAudioSource(0, audioSource);
        }
        else
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        }

        // Assign video clip
        videoPlayer.clip = clip;

        // Only register ONCE
        videoPlayer.prepareCompleted -= OnVideoPrepared;
        videoPlayer.prepareCompleted += OnVideoPrepared;

        videoPlayer.Prepare();
        isVideoMode = true;
    }

    private IEnumerator VideoDwellThenPlay()
    {
        // wait until player has continuously stayed inside for the dwell duration
        // this prevents the user from triggering the video to start and then leaving
        // or having any quick passthroughs be the source of the video starting and stopping quickl
        float t = 0f;
        while (t < videoDwellSeconds)
        {
            if (insideCount <= 0) yield break;   // left before dwell finished
            t += Time.deltaTime;
            yield return null;
        }

        // still inside after dwell? play
        if (insideCount > 0)
            PlayPreparedVideo();

        dwellRoutine = null;
    }

    void VolumeChanged()
    {
        if (audioSource != null)
        {
            audioSource.volume = GlobalSettings.MASTER_VOLUME;
        }
    }

    // this is for button play/pause
    // will disable the play on proximity for that frame once pressed once,  
    public void ToggleVideoPlayback()
    {
        if (!isVideoMode || videoPlayer == null)
            return;
        // debug
        //if (!videoPlayer.isPrepared)
        //{
        //    Debug.Log("Video not ready yet.");
        //    return;
        //}

        playVideoOnPlayerProximity = false;

        if (videoPlayer.isPlaying)
        {
            PausePreparedVideo();
        }
        else
        {
            PlayPreparedVideo();
        }
    }

    /* -------------------------------------------------------------- *
     *                   Trigger Handling(Colliders)                  *
     * -------------------------------------------------------------- */

    private bool isCollisionTag(Collider other)
    {
        return other.CompareTag(collisionTag);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!playVideoOnPlayerProximity) return;
        if (!isVideoMode) return;                 // only care if current media is a video
        if (!isCollisionTag(other)) return;

        insideCount++;

        // start (or restart) dwell timer when someone enters
        if (dwellRoutine != null) StopCoroutine(dwellRoutine);
        dwellRoutine = StartCoroutine(VideoDwellThenPlay());
    }

    private void OnTriggerExit(Collider other)
    {
        if (!playVideoOnPlayerProximity) return;
        if (!isVideoMode) return;
        if (!isCollisionTag(other)) return;

        insideCount = Mathf.Max(0, insideCount - 1);

        // if nobody left inside, cancel dwell + optionally pause
        if (insideCount == 0)
        {
            if (dwellRoutine != null)
            {
                StopCoroutine(dwellRoutine);
                dwellRoutine = null;
            }

            if (pauseOnExit)
                PausePreparedVideo();
        }
    }
}
