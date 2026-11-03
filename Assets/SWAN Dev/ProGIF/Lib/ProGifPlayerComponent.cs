// Created by SwanDEV 2017

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

#if UNITY_2017_3_OR_NEWER
using UnityEngine.Networking;
#endif

using ThreadPriority = System.Threading.ThreadPriority;

[DisallowMultipleComponent]
public abstract class ProGifPlayerComponent : MonoBehaviour
{
    private bool debugLog = false;

    private bool _ignoreTimeScale = true;   // Default: true;
    public bool ignoreTimeScale
    {
        get
        {
            return _ignoreTimeScale;
        }
        set
        {
            _ignoreTimeScale = value;
            nextFrameTime = value ? Time.unscaledTime : Time.time;
        }
    }

    public string loadPath;

    public int totalFrame = 0;

    public ProGifDecoder.GifInfo m_GifInfo = null;

    [HideInInspector] public List<GifTexture> gifTextures = new List<GifTexture>();

    [HideInInspector] public DisplayType displayType = DisplayType.None;    // Indicates the display target is an Image, Renderer, or GUITexture

    [HideInInspector] public float nextFrameTime = 0.0f;                    // The game time to show next frame
    [HideInInspector] public int spriteIndex = 0;                           // The current sprite index to be played

    /// <summary> Textures filter mode </summary> 
    [SerializeField]
    private FilterMode m_filterMode = FilterMode.Point;
    /// <summary> Textures wrap mode </summary> 
    [SerializeField]
    private TextureWrapMode m_wrapMode = TextureWrapMode.Clamp;

    /// <summary> Indicates the gif frames is loaded from recorder or decoder </summary> 
    private bool isDecoderSource = false;

    /// <summary> Sets the worker threads priority. This will only affect newly created threads. </summary>
    public ThreadPriority m_workerPriority = ThreadPriority.BelowNormal;

    /// <summary> Gets the progress when load Gif from path/url. </summary>
    public float LoadingProgress
    {
        get
        {
            return (float)gifTextures.Count / (float)totalFrame;
        }
    }

    public bool IsLoadingComplete
    {
        get
        {
            return LoadingProgress >= 1f;
        }
    }

    /// <summary> This component state enum </summary>
    public enum PlayerState
    {
        None,
        Loading,
        Ready,
        Playing,
        Pause,
    }

    private PlayerState _state;
    /// <summary> Current state </summary>
    public PlayerState State
    {
        get
        {
            return _state;
        }
        private set
        {
            _state = value;
            nextFrameTime = ignoreTimeScale ? Time.unscaledTime : Time.time;
        }
    }
    public void SetState(PlayerState state)
    {
        State = state;
    }

    /// <summary> Animation loop count (0 is infinite) </summary>
    public int loopCount
    {
        get;
        private set;
    }

    /// <summary> Texture width (px) </summary>
    public int width
    {
        get;
        private set;
    }

    /// <summary> Texture height (px) </summary>
    public int height
    {
        get;
        private set;
    }

    /// <summary> Default waiting time among frames. </summary> 
    private float _interval = 0.1f;
    /// <summary> Get the current frame waiting time. </summary> 
    public float interval
    {
        get
        {
            if (gifTextures.Count <= 0) return 0.1f;
            // For GIF that the delaySec is incorrectly set, we use 0.1f as its delaySec (or 10 FPS), which is one of the widely used framerate for GIF.
            return (gifTextures[spriteIndex].m_delaySec <= 0.0166f) ? 0.1f : gifTextures[spriteIndex].m_delaySec; // Here we let the maximum FPS of GIF be 60, while in the GIF specification it is 30. 
        }
    }

    /// <summary> Current playback speed of the GIF. </summary> 
    private float _playbackSpeed = 1.0f;
    /// <summary> Get/Set the playback speed of the GIF. (Default is 1.0f) </summary> 
    public float playbackSpeed
    {
        get
        {
            return _playbackSpeed;
        }
        set
        {
            float prevInterval = interval / _playbackSpeed;

            _playbackSpeed = Mathf.Max(0.01f, value);

            // update the next frame time
            float time = ignoreTimeScale ? Time.unscaledTime : Time.time;
            float timeLeftPercent = nextFrameTime > time ? (nextFrameTime - time) / prevInterval : 0f;
            nextFrameTime = time + (interval / _playbackSpeed) * timeLeftPercent;
        }
    }

    [HideInInspector] public bool shouldSaveFromWeb = false;                // True: save file download from web

    public enum DisplayType
    {
        None = 0,
        Image,
        Renderer,
        GuiTexture,
        RawImage,
    }

    //Decode settings
    public enum DecodeMode
    {
        /// <summary> Decode all gif frames. </summary>
        Normal = 0,

        /// <summary> Decode gif by skipping some frames, targetDecodeFrameNum is the number of frames to decode. </summary>
        Advanced,
    }
    public enum FramePickingMethod
    {
        /// <summary> Decode gif frame by frame from first to the target number(targetDecodeFrameNum). </summary>
        ContinuousFromBeginning = 0,

        /// <summary> Decode a target amount(targetDecodeFrameNum) of gif frames(skip frames by an averaged interval). </summary>
        AverageInterval,

        /// <summary> Decode the first half of the gif frames(not more than targetDecodeFrameNum if provided targetDecodeFrameNum larger than 0). </summary>
        OneHalf,

        /// <summary> Decode the first one-third of the gif frames(not more than targetDecodeFrameNum if provided targetDecodeFrameNum larger than 0). </summary>
        OneThird,

        /// <summary> Decode the first one-fourth of the gif frames(not more than targetDecodeFrameNum if provided targetDecodeFrameNum larger than 0). </summary>
        OneFourth
    }
    public enum Decoder
    {
        ProGif_QueuedThread = 0,
        ProGif_Coroutines,
    }

    protected ProGifDecoder proGifDecoder;

    //Advanced settings ------------------
    /// <summary> If 'True', use the settings on the prefab, this will ignore changes from PGif/ProGifManager. </summary> 
    [Header("[ Advanced Decode Settings ]")]
    [Tooltip("If 'True', use the settings on the prefab, this will ignore changes from PGif/ProGifManager.")]
    public bool UsePresetSettings = false;

    public Decoder decoder = Decoder.ProGif_QueuedThread;

    public DecodeMode decodeMode = DecodeMode.Normal;

    public FramePickingMethod framePickingMethod = FramePickingMethod.ContinuousFromBeginning;

    public int targetDecodeFrameNum = -1;   //if targetDecodeFrameNum <= 0: decode & play all frames (+/- 1 frame)

    /// <summary> Set to 'true' to take advantage of the highly optimized ProGif playback solution for significantly save the memory usage. </summary> 
    public bool optimizeMemoryUsage = true;
    //Advanced settings ------------------

    /// <summary> Resets the decode settings(Set the decodeMode as Normal, simply decodes the entire gif without applying advanced settings) </summary>
    public void ResetDecodeSettings()
    {
        if (UsePresetSettings)
        {
#if UNITY_EDITOR
            Debug.Log("UsePresetSettings is selected, the decoder will use the settings on the prefab and ignore changes from PGif/ProGifManager.");
#endif
            return;
        }
        decoder = Decoder.ProGif_QueuedThread;
        decodeMode = ProGifPlayerComponent.DecodeMode.Normal;
        framePickingMethod = ProGifPlayerComponent.FramePickingMethod.ContinuousFromBeginning;
        targetDecodeFrameNum = -1;
        optimizeMemoryUsage = true;
    }

    /// <summary> Sets the decodeMode as Advanced, apply the advanced settings(targetDecodeFrameNum, framePickingMethod..) </summary>
    public void SetAdvancedDecodeSettings(Decoder decoder, int targetDecodeFrameNum = -1, FramePickingMethod framePickingMethod = FramePickingMethod.ContinuousFromBeginning, bool optimizeMemoryUsage = true)
    {
        if (UsePresetSettings)
        {
#if UNITY_EDITOR
            Debug.Log("UsePresetSettings is selected, the decoder will use the settings on the prefab and ignore changes from PGif/ProGifManager.");
#endif
            return;
        }
        this.decoder = decoder;
        this.decodeMode = DecodeMode.Advanced;
        this.framePickingMethod = framePickingMethod;
        this.targetDecodeFrameNum = targetDecodeFrameNum;
        this.optimizeMemoryUsage = optimizeMemoryUsage;
    }

    /// <summary> Indicates if loading file from local or Web. </summary>
    private bool _loadingFile = false;
    void OnEnable()
    {
        if (!_loadingFile && _gifBytes == null && !string.IsNullOrEmpty(loadPath))
        {
            Play(loadPath, shouldSaveFromWeb);
        }
    }

    private byte[] _gifBytes = null;
    public byte[] GetBytes()
    {
        return _gifBytes;
    }

    public void SetBytes(byte[] bytes, bool play = false)
    {
        _gifBytes = bytes;
        if (play) PlayWithLoadedBytes(false);
    }

    public void ClearBytes()
    {
        _gifBytes = null;
    }

    public void PlayWithLoadedBytes(bool clearCallbacks = false)
    {
        if (_gifBytes == null) return;
        shouldSaveFromWeb = false;
        Clear(false, clearCallbacks);
        gifTextures = new List<GifTexture>();
        _PlayWithBytes(_gifBytes);
    }

    public void Play(string loadPath, bool shouldSaveFromWeb)
    {
        this.shouldSaveFromWeb = shouldSaveFromWeb;
        Clear();
        gifTextures = new List<GifTexture>();
        LoadGifFromUrl(loadPath);
        this.loadPath = loadPath;
    }

    /// <summary> Setup to play the stored textures from gif recorder. </summary>
    public virtual void Play(RenderTexture[] gifFrames, float fps, bool isCustomRatio, int customWidth, int customHeight, bool optimizeMemoryUsage)
    {
        gifTextures = new List<GifTexture>();
        
        this.optimizeMemoryUsage = optimizeMemoryUsage;

        isDecoderSource = false;

        _interval = 1.0f / fps;

        m_GifInfo = new ProGifDecoder.GifInfo()
        {
            m_FPS = fps,
        };

        Clear();

        totalFrame = gifFrames.Length;

        StartCoroutine(_AddGifTextures(gifFrames, fps, isCustomRatio, customWidth, customHeight, optimizeMemoryUsage, 0, yieldPerFrame: true));

        StartCoroutine(_DelayCallback());

        State = PlayerState.Playing;
    }

    private IEnumerator _AddGifTextures(RenderTexture[] gifFrames, float fps, bool isCustomRatio, int customWidth, int customHeight, bool optimizeMemory, int currentIndex, bool yieldPerFrame)
    {
        int i = currentIndex;

        if (isCustomRatio)
        {
            width = customWidth;
            height = customHeight;
            Texture2D tex = new Texture2D(width, height);
            RenderTexture.active = gifFrames[i];
            tex.ReadPixels(new Rect((gifFrames[i].width - tex.width) / 2, (gifFrames[i].height - tex.height) / 2, tex.width, tex.height), 0, 0);
            tex.Apply();
            gifTextures.Add(new GifTexture(tex, _interval, optimizeMemory));
        }
        else
        {
            width = gifFrames[0].width;
            height = gifFrames[0].height;
            Texture2D tex = new Texture2D(gifFrames[i].width, gifFrames[i].height);
            RenderTexture.active = gifFrames[i];
            tex.ReadPixels(new Rect(0.0f, 0.0f, gifFrames[i].width, gifFrames[i].height), 0, 0);
            tex.Apply();
            gifTextures.Add(new GifTexture(tex, _interval, optimizeMemory));
        }

        if (currentIndex == 1) OnLoading(LoadingProgress);

        if (yieldPerFrame) yield return new WaitForEndOfFrame();

        if (OnLoading != null) OnLoading(LoadingProgress);

        currentIndex++;

        if (currentIndex < gifFrames.Length)
        {
            StartCoroutine(_AddGifTextures(gifFrames, fps, isCustomRatio, customWidth, customHeight, optimizeMemory, currentIndex, yieldPerFrame));
        }
        else
        {
            // Texture import finished
        }
    }

    private IEnumerator _DelayCallback()
    {
        yield return new WaitForEndOfFrame();
        _OnFrameReady(gifTextures[0], true);
        if (gifTextures != null && gifTextures.Count > 0) _OnFirstFrameReady(gifTextures[0]);
    }

    public void Pause()
    {
        State = PlayerState.Pause;
    }

    public void Resume()
    {
        State = PlayerState.Playing;
    }

    public void Stop()
    {
        State = PlayerState.Pause;
        spriteIndex = 0;
    }

    /// <summary>
    /// Set GIF texture from url
    /// </summary>
    /// <param name="url">GIF image url (Web link or local path)</param>
    public void LoadGifFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return;
        StartCoroutine(LoadGifRoutine(url, (gifBytes) =>
        {
            _PlayWithBytes(gifBytes);
        }));
    }

    public IEnumerator LoadGifRoutine(string url, Action<byte[]> onLoaded)
    {
        if (string.IsNullOrEmpty(url))
        {
#if UNITY_EDITOR
            Debug.LogError("URL is nothing.");
#endif
            yield break;
        }

        if (State == PlayerState.Loading)
        {
            Debug.LogWarning("Already loading.");
            yield break;
        }
        State = PlayerState.Loading;

        FilePathName filePathName = FilePathName.Instance;

        bool isFromWeb = false;
        string path = url;
        if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            // from WEB
            isFromWeb = true;
        }
        else if (path.StartsWith("/idbfs/", StringComparison.OrdinalIgnoreCase))
        {
            // from WebGL index DB
            _gifBytes = filePathName.ReadFileToBytes(path);
        }
        else
        {
            // from Local
            path = filePathName.EnsureLocalPath(path);

#if UNITY_EDITOR
            Debug.Log("(ProGifPlayerComponent) Local file path: " + path);
#endif
        }

        if (_gifBytes != null)
        {
            onLoaded(_gifBytes);
        }
        else
        {
            // Load file
            _loadingFile = true;

#if UNITY_2017_3_OR_NEWER
            using (UnityWebRequest uwr = UnityWebRequest.Get(path))
            {
                uwr.SendWebRequest();
                while (!uwr.isDone) yield return 1;

#if UNITY_2020_1_OR_NEWER
                if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError || uwr.result == UnityWebRequest.Result.DataProcessingError)
#else
                if (uwr.isNetworkError || uwr.isHttpError)
#endif
                {
#if UNITY_EDITOR
                    Debug.LogError("File load error.\n" + uwr.error);
#endif
                    State = PlayerState.None;
                    yield break;
                }
                else
                {
                    _gifBytes = uwr.downloadHandler.data;

                    onLoaded(_gifBytes);

                    //Save bytes to gif file if it is downloaded from web
                    if (isFromWeb && shouldSaveFromWeb)
                    {
                        filePathName.FileStreamTo(filePathName.GetDownloadedGifSaveFullPath(), _gifBytes);
                    }
                }
            }
#else
            using (WWW www = new WWW(path))
            {
                yield return www;
                if (string.IsNullOrEmpty(www.error) == false)
                {
#if UNITY_EDITOR
                    Debug.LogError("File load error.\n" + www.error);
#endif
                    State = PlayerState.None;
                    yield break;
                }
                _loadingFile = false;

                State = PlayerState.Loading; // PlayerState.Loading = Decoding

                _gifBytes = www.bytes;

                onLoaded(_gifBytes);

                //Save bytes to gif file if it is downloaded from web
                if (isFromWeb && shouldSaveFromWeb)
                {
                    filePathName.FileStreamTo(filePathName.GetDownloadedGifSaveFullPath(), _gifBytes);
                }
            }
#endif
        }
    }

    private void _PlayWithBytes(byte[] gifBytes)
    {
        isFirstFrame = true;

#if UNITY_EDITOR
        Debug.Log((decoder == Decoder.ProGif_QueuedThread ? "Decode process run in Threads: " : "Decode process run in Coroutines: ") + gameObject.name);
        startDecodeTime = Time.time;
#endif

        isDecoderSource = true;

        if (decoder == Decoder.ProGif_QueuedThread) // decode in worker thread
        {
            currentDecodeIndex = 0;
            decodeCompletedFlag = false;

            if (proGifDecoder != null)
            {
                ProGifDeWorker.GetInstance().DeQueueDecoder(proGifDecoder);
            }

            proGifDecoder = new ProGifDecoder(_gifBytes,
                (gifTexList, loop, w, h) =>
                {
                    if (gifTexList != null)
                    {
                        this.loopCount = loop;
                        this.width = w;
                        this.height = h;
                        decodeCompletedFlag = true;
                    }
                    else
                    {
                        State = PlayerState.None;
                    }
                },
                m_filterMode, m_wrapMode, debugLog,
                (gTex) =>
                {
                    _AddGifTexture(gTex);
                },
                (gifInfo) =>
                {
                    m_GifInfo = gifInfo;
                    totalFrame = gifInfo.m_TotalFrame;
                }
            );

            if (decodeMode == DecodeMode.Normal) proGifDecoder.ResetDecodeSettings();
            else proGifDecoder.SetAdvancedDecodeSettings(targetDecodeFrameNum, framePickingMethod);

            proGifDecoder.SetOptimizeMemoryUsgae(optimizeMemoryUsage);

            ProGifDeWorker.GetInstance(m_workerPriority).QueueDecoder(proGifDecoder);
            ProGifDeWorker.GetInstance().Start();
        }
        else // decode in coroutine
        {
            proGifDecoder = new ProGifDecoder(_gifBytes,
                (gifTexList, loop, w, h) =>
                {
                    if (gifTexList != null)
                    {
#if UNITY_EDITOR
                        Debug.Log(gameObject.name + " - Total Decode Time: " + (Time.time - startDecodeTime));
#endif
                        this.loopCount = loop;
                        this.width = w;
                        this.height = h;

                        _UnlockColors(gifTexList);

                        _OnComplete();
                    }
                    else
                    {
#if UNITY_EDITOR
                        Debug.LogError("Gif texture get error.");
#endif
                        State = PlayerState.None;
                    }
                },
                m_filterMode, m_wrapMode, debugLog,
                (gTex) =>
                {
                    _AddGifTexture(gTex);
                    _OnFrameReady(gTex, isFirstFrame);
                    if (isFirstFrame) _OnFirstFrameReady(gTex);
                    if (OnLoading != null) OnLoading(LoadingProgress);

                    isFirstFrame = false;
                },
                (gifInfo) =>
                {
                    m_GifInfo = gifInfo;
                    totalFrame = gifInfo.m_TotalFrame;
                }
            );

            if (decodeMode == DecodeMode.Normal) proGifDecoder.ResetDecodeSettings();
            else proGifDecoder.SetAdvancedDecodeSettings(targetDecodeFrameNum, framePickingMethod);

            proGifDecoder.SetOptimizeMemoryUsgae(optimizeMemoryUsage);

            StartCoroutine(proGifDecoder.GetTextureListCoroutine());
        }
    }

    bool decodeCompletedFlag = false;
    float startDecodeTime = 0f;
    int currentDecodeIndex = 0;
    bool isFirstFrame = true;
    /// Update for decoder using thread.
    protected void ThreadsUpdate()
    {
        if (!isDecoderSource || decoder != Decoder.ProGif_QueuedThread) return;

        if (currentDecodeIndex < gifTextures.Count)
        {
            currentDecodeIndex++;
            _OnFrameReady(gifTextures[currentDecodeIndex - 1], isFirstFrame);
        }

        if (isFirstFrame && gifTextures.Count > 0)
        {
            isFirstFrame = false;
            _OnFirstFrameReady(gifTextures[0]);
        }

        if (OnLoading != null && gifTextures.Count > 0) OnLoading(LoadingProgress);

        if (decodeCompletedFlag)
        {
#if UNITY_EDITOR
            Debug.Log(gameObject.name + " - Total Decode Time: " + (Time.time - startDecodeTime));
#endif
            decodeCompletedFlag = false;

            _UnlockColors(gifTextures);

            _OnComplete();
        }
    }

    // Optional to override
    protected virtual void _AddGifTexture(GifTexture gTex)
    {
        gifTextures.Add(gTex);
    }

    /// <summary>
    /// This is called on every gif frame decode finish
    /// </summary>
    /// <param name="gTex">GifTexture.</param>
    protected abstract void _OnFrameReady(GifTexture gTex, bool isFirstFrame);

    public void _OnFirstFrameReady(GifTexture gifTex)
    {
        State = PlayerState.Playing;
        _interval = gifTex.m_delaySec;
        width = gifTex.m_Width;
        height = gifTex.m_Height;
        if (OnFirstFrame != null)
        {
            OnFirstFrame(new FirstGifFrame()
            {
                gifTexture = gifTex,
                width = this.width,
                height = this.height,
                interval = this.interval,
                totalFrame = this.totalFrame,
                loopCount = m_GifInfo.m_LoopCount,
                byteLength = m_GifInfo.m_ByteLength,
                fps = m_GifInfo.m_FPS,
            });
        }
    }

    private void _OnComplete()
    {
        if (OnDecodeComplete != null)
        {
            OnDecodeComplete(new DecodedResult()
            {
                gifTextures = this.gifTextures,
                loopCount = this.loopCount,
                width = this.width,
                height = this.height,
                interval = this.interval,
                totalFrame = this.totalFrame,
            });
        }
    }

    public Action<FirstGifFrame> OnFirstFrame = null;
    public void SetOnFirstFrameCallback(Action<FirstGifFrame> onFirstFrame)
    {
        OnFirstFrame = onFirstFrame;
    }

    public class FirstGifFrame
    {
        public GifTexture gifTexture;
        public int width;
        public int height;
        public float interval;
        public int totalFrame;
        public int loopCount = -1; // -1 = no loop, 0 = loop
        public int byteLength = 0;
        public float fps = 0;
    }

    public Action<float> OnLoading = null;
    public void SetLoadingCallback(Action<float> onLoading)
    {
        OnLoading = onLoading;
    }

    public Action<DecodedResult> OnDecodeComplete = null;
    public void SetOnDecodeCompleteCallback(Action<DecodedResult> onDecodeComplete)
    {
        OnDecodeComplete = onDecodeComplete;
    }

    public class DecodedResult
    {
        public List<GifTexture> gifTextures;
        public int width;
        public int height;
        public float interval;
        public int loopCount;
        public int totalFrame;

        public float fps
        {
            get
            {
                return 1f / interval;
            }
        }
    }

    public Action<GifTexture> OnPlayingCallback = null;
    public void SetOnPlayingCallback(Action<GifTexture> onPlayingCallback)
    {
        OnPlayingCallback = onPlayingCallback;
    }

    /// <summary>
    /// Sets the flag to clear the colors in the GifTexture list
    /// </summary>
    protected void _UnlockColors(List<GifTexture> gifTexList)
    {
        if (gifTexList != null)
        {
            for (int i = 0; i < gifTexList.Count; i++)
            {
                if (gifTexList[i] != null)
                {
                    gifTexList[i].m_LockColorData = false;
                }
            }
        }
    }

    /// <summary>
    /// Clear the sprite, texture2D and colors in the GifTexture list
    /// </summary>
    protected void _ClearGifTextures(List<GifTexture> gifTexList)
    {
        if (gifTexList != null)
        {
            for (int i = 0; i < gifTexList.Count; i++)
            {
                if (gifTexList[i] != null)
                {
                    gifTexList[i].m_Colors = null;

                    if (gifTexList[i].m_texture2d != null)
                    {
                        Destroy(gifTexList[i].m_texture2d);
                        gifTexList[i].m_texture2d = null;
                    }

                    if (gifTexList[i].m_Sprite != null && gifTexList[i].m_Sprite.texture != null)
                    {
                        Destroy(gifTexList[i].m_Sprite.texture);
                        Destroy(gifTexList[i].m_Sprite);
                        gifTexList[i].m_Sprite = null;
                    }
                }
            }
        }
    }

    private void OnDestroy()
    {
        Clear();
    }

    public virtual void Clear(bool clearBytes = true, bool clearCallbacks = true)
    {
        State = PlayerState.None;
        spriteIndex = 0;
        nextFrameTime = 0f;

        // Clear callbacks
        if (clearCallbacks)
        {
            OnLoading = null;
            OnFirstFrame = null;
            OnDecodeComplete = null;
            OnPlayingCallback = null;
        }

        StopAllCoroutines();

        if (clearBytes)
        {
            ClearBytes();
        }

        //Clear gifTextures in loading coroutines/threads
        if (proGifDecoder != null)
        {
            ProGifDeWorker.GetInstance().DeQueueDecoder(proGifDecoder);
        }

        //Clear gifTextures of the PlayerComponent
        _ClearGifTextures(gifTextures);
    }

    
    //-- Resize --------
    //private int newFps = -1;
    //private Vector2 newSize = Vector2.zero;
    //private bool keepRatioForNewSize = true;
    //public void Resize_AdvancedMode(GifTexture gTex)
    //{
    //    ImageResizer imageResizer = null;
    //    bool reSize = false;
    //    if (newSize.x > 0 && newSize.y > 0 && decodeMode == ProGifPlayerComponent.DecodeMode.Advanced)
    //    {
    //        imageResizer = new ImageResizer();
    //        reSize = true;
    //    }

    //    if (reSize) gTex.m_texture2d = (keepRatioForNewSize) ?
    //             imageResizer.ResizeTexture32_KeepRatio(gTex.m_texture2d, (int)newSize.x, (int)newSize.y) :
    //             imageResizer.ResizeTexture32(gTex.m_texture2d, (int)newSize.x, (int)newSize.y);
    //}
    //-- Resize ----------------

}
