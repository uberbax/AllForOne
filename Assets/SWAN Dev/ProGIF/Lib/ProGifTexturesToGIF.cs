// Created by SwanDEV 2017

using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using ThreadPriority = System.Threading.ThreadPriority;

/// <summary>
/// Converts Texture2D(s) or RenterTexture(s) to GIF. 
/// </summary>
public class ProGifTexturesToGIF : MonoBehaviour
{
    private static Dictionary<string, ProGifTexturesToGIF> _GifConverterDict = new Dictionary<string, ProGifTexturesToGIF>();

    /// <summary>
    /// Gets the converter by converterName (that input in the Create method)
    /// </summary>
    /// <param name="converterName">Converter name (unique)</param>
    /// <returns></returns>
    public static ProGifTexturesToGIF GetConverter(string converterName)
    {
        ProGifTexturesToGIF converter = null;
        _GifConverterDict.TryGetValue(converterName, out converter);
        if (converter == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("GetConverter - Converter not found: " + converterName);
#endif
        }
        return converter;
    }

    /// <summary>
    /// Name of this converter, use as the key for the Dictionary : _GifConverterDict.
    /// </summary>
    public string m_ConverterName = "DefaultConverter";

    /// <summary>
    /// Resized textures, can be used for preview purposes. (See: m_ResolutionHandle)
    /// </summary>
    public List<Texture2D> m_PreviewTextures = new List<Texture2D>();

    /// <summary>
    /// Sets the worker threads priority. This will only affect newly created threads.
    /// </summary>
    public ThreadPriority m_WorkerPriority = ThreadPriority.BelowNormal;

    /// <summary>
    /// The playback mode for encoding frames to GIF file.
    /// Available modes including Forward, Backward and Ping-pong mode, Ping-pong mode will double the frames in the created GIF file.
    /// </summary>
    public ProGifRecorderComponent.EncodePlayMode m_EncodePlayMode = ProGifRecorderComponent.EncodePlayMode.Normal;

    /// <summary> The transparent color to hide in the GIF. </summary>
    [SerializeField]
    private Color32 m_TransparentColor = new Color32(0, 0, 0, 0);
    /// <summary> The range of RGB value for picking nearby colors of the input color to set as transparent pixels. </summary>
    [SerializeField]
    byte m_TransparentColorRange = 0;

    /// <summary> If 'TRUE', check if any pixels' alpha value equal zero and auto encode GIF with transpareny. </summary>
    [SerializeField]
    private bool m_AutoTransparent = false;

    /// <summary>
    /// Limits the number of threads for encoding the GIF. Less or equals 0: use all the CPU threads.
    /// (To check the max threads support on the current device, use UnityEngine.SystemInfo.processorCount)
    /// </summary>
    public int m_MaxNumberOfThreads = 0;

    public ResolutionHandle m_ResolutionHandle = ResolutionHandle.ResizeKeepRatio;
    public enum ResolutionHandle
    {
        Resize = 0,
        ResizeKeepRatio,
    }

    /// <summary>
    /// Sets the GIF rotation (Support rotate 0, -90, 90, 180 degrees).
    /// </summary>
    public ImageRotator.Rotation m_Rotation = ImageRotator.Rotation.None;

    /// <summary>
    /// Sets the transparent color, hide this color in the GIF. 
    /// The GIF specification allows setting a color to be transparent. 
    /// *** Use case: if you want to record gameObject, character or anything else with transparent background, 
    /// please make sure the background is of solid color(no gradient), and the target object do not contain this color.
    /// (Also be reminded, the transparent feature takes more time to encoding the GIF)
    /// </summary>
    /// <param name="color32">The Color to hide in the gif. Make sure the alpha value greater than Zero, else disable the transparent feature.</param>
    /// <param name="transparentColorRange">The range of RGB value for picking nearby colors of the input color to set as transparent pixels.</param>
    public void SetTransparent(Color32 color32)
    {
        m_TransparentColor = color32;
    }

    /// <summary>
    /// Auto detects the input image(s) pixels for enable/disable transparent feature. 
    /// *** Use case: for pre-made images that have transparent pixels manually set.
    /// (Also be reminded, the transparent feature takes more time to encoding the GIF)
    /// </summary>
    /// <param name="autoDetectTransparent">If set to <c>true</c> auto detect transparent pixels to enable the transparent feature, else disable the auto detection.</param>
    public void SetTransparent(bool autoDetectTransparent)
    {
        m_AutoTransparent = autoDetectTransparent;
    }

    public void SetFileName(string fileNameWithoutExtension)
    {
        _providedFileName = fileNameWithoutExtension;
    }

    private string FileName
    {
        get
        {
            return (_providedFileName == string.Empty) ? FilePathName.Instance.GetGifFileName() : _providedFileName;
        }
    }

    private string SaveFolder
    {
        get
        {
            return FilePathName.Instance.GetSaveDirectory();
        }
    }

    private string _providedFileName = string.Empty;
    private ImageResizer _imageResizer = new ImageResizer();
    private float frameDelay_Override = 0f;


    private static ProGifTexturesToGIF _instance;
    public static ProGifTexturesToGIF Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Create("[ProGifTexturesToGIF]");
            }
            return _instance;
        }
    }

    /// <summary>
    /// Creates a new converter.
    /// </summary>
    /// <param name="converterName">Converter name (unique)</param>
    /// <returns></returns>
    public static ProGifTexturesToGIF Create(string converterName)
    {
        ProGifTexturesToGIF tex2Gif = new GameObject(converterName).AddComponent<ProGifTexturesToGIF>();
        tex2Gif.m_ConverterName = converterName;

        //Add the new converter to dictionary
        if (_GifConverterDict.ContainsKey(converterName))
        {
            _GifConverterDict[converterName] = tex2Gif;
        }
        else
        {
            _GifConverterDict.Add(converterName, tex2Gif);
        }

        return tex2Gif;
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
    }

    private int workerId = 0;
    private float saveProgress = 0.0f;
    private string filePath = string.Empty;     //the file save path to be returned on file saved
    private bool invokeFileProgress = false;
    private bool invokeFileSaved = false;

    private int numberOfThreads = 1;
    private List<ProGifWorker> workers;
    private int framesFinished;
    private System.Diagnostics.Stopwatch stopwatch;

    /// <summary>
    /// Called by each worker thread every time a frame is processed during the save process.
    /// The first parameter holds the worker ID and the second one a value in range [0;1] for
    /// the actual progress. This callback is probably not thread-safe, use at your own risks.
    /// </summary>
    public event Action<int, float> OnFileSaveProgress = delegate { };

    /// <summary>
    /// Called once a gif file has been saved. The first parameter will hold the worker ID and
    /// the second one the absolute file path.
    /// </summary>
    public event Action<int, string> OnFileSaved = delegate { };

    private Action<Color32[], int, int> _OnSaveFirstFrame = null;
    public void SetOnSaveFirstFrame(Action<Color32[], int, int> onSaveFirstFrame)
    {
        _OnSaveFirstFrame = onSaveFirstFrame;
    }

    private void Update()
    {
        if (invokeFileProgress)
        {
            invokeFileProgress = false;
            OnFileSaveProgress(workerId, saveProgress);
        }

        if (invokeFileSaved)
        {
            Debug.Log("invokeFileSaved - workerId: " + workerId + " filePath: " + filePath);
            invokeFileSaved = false;
            OnFileSaved(workerId, filePath);
        }
    }

    private void FileSaved(int id, string path)
    {
        this.workerId = id;
        this.filePath = path;
        this.invokeFileSaved = true;
    }

    private int frameCountFinal;
    private void FileSaveProgress(int id)
    {
        this.workerId = id;
        this.invokeFileProgress = true;
        this.framesFinished++;
        this.saveProgress = (float)framesFinished / frameCountFinal;
    }

    private int jobsFinished;
    private void EncoderFinished(int encoderIndex)
    {
        jobsFinished++;
        if (jobsFinished == numberOfThreads)
        {
            JobsFinished();
        }
    }

    private void JobsFinished()
    {
        FileStream fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
        for(int i=0; i < workers.Count; i++)
        {
            MemoryStream stream = workers[i].m_Encoder.GetMemoryStream();
            stream.Position = 0;
            stream.WriteTo(fileStream);
            stream.Close();
        }
        fileStream.Close();

        FileSaved(workerId, filePath);

#if UNITY_EDITOR
        Debug.Log("All jobs finished, total encode time: " + stopwatch.Elapsed);
#endif
        stopwatch.Stop();
    }

    /// <summary>
    /// Convert and save a List of Texture2D to GIF.
    /// </summary>
    /// <param name="textureList">Texture list.</param>
    /// <param name="width">Target width for the GIF.</param>
    /// <param name="height">Target height for thr GIF.</param>
    /// <param name="fps">Frame count per second.</param>
    /// <param name="loop">Repeat time, -1: no repeat, 0: infinite, >0: repeat count.</param>
    /// <param name="quality">Quality, (1 - 100) 1: best(larger storage size), 100: faster(smaller storage size)</param>
    /// <param name="onFileSaved">On file saved callback.</param>
    /// <param name="onFileSaveProgress">On file save progress callback.</param>
    /// <param name="resolutionHandle">The method to resize the textures if their size different to the target size.</param>
    /// <param name="autoClear">If set to <c>true</c>, clear this instance and textures when the GIF is saved.</param>
	/// <param name="destroyOriginTexture">If set to <c>true</c>, clear the original textures after processed.</param>
	/// <param name="smooth_yieldPerFrame">Process each texture in separate frame to avoid blocking the main thread, so the UI smoother.</param>
    public string Save(List<Texture2D> textureList, int width, int height, int fps, int loop, int quality,
        Action<int, string> onFileSaved = null, Action<int, float> onFileSaveProgress = null,
        ResolutionHandle resolutionHandle = ResolutionHandle.ResizeKeepRatio, bool autoClear = true, bool destroyOriginTexture = false, bool smooth_yieldPerFrame = false)
    {
        this.m_ResolutionHandle = resolutionHandle;
        return _Save(textureList, width, height, fps, loop, quality, onFileSaved, onFileSaveProgress, autoClear, destroyOriginTexture, smooth_yieldPerFrame);
    }

    /// <summary>
    /// Convert and save a List of Texture2D to GIF.
    /// </summary>
    /// <param name="textureList">Texture list.</param>
    /// <param name="width">Target width for the GIF.</param>
    /// <param name="height">Target height for thr GIF.</param>
    /// <param name="frameDelay">Frame delay time in seconds.</param>
    /// <param name="loop">Repeat time, -1: no repeat, 0: infinite, >0: repeat count.</param>
    /// <param name="quality">Quality, (1 - 100) 1: best(larger storage size), 100: faster(smaller storage size)</param>
    /// <param name="onFileSaved">On file saved callback.</param>
    /// <param name="onFileSaveProgress">On file save progress callback.</param>
    /// <param name="resolutionHandle">The method to resize the textures if their size different to the target size.</param>
    /// <param name="autoClear">If set to <c>true</c>, clear this instance and textures when the GIF is saved.</param>
    /// <param name="destroyOriginTexture">If set to <c>true</c>, clear the original textures after processed.</param>
    /// <param name="smooth_yieldPerFrame">Process each texture in separate frame to avoid blocking the main thread, so the UI smoother.</param>
    public string Save(List<Texture2D> textureList, int width, int height, float frameDelay, int loop, int quality,
        Action<int, string> onFileSaved = null, Action<int, float> onFileSaveProgress = null,
        ResolutionHandle resolutionHandle = ResolutionHandle.ResizeKeepRatio, bool autoClear = true, bool destroyOriginTexture = false, bool smooth_yieldPerFrame = false)
    {
        frameDelay_Override = frameDelay;
        this.m_ResolutionHandle = resolutionHandle;
        return _Save(textureList, width, height, 0, loop, quality, onFileSaved, onFileSaveProgress, autoClear, destroyOriginTexture, smooth_yieldPerFrame);
    }

    private string _Save(List<Texture2D> textureList, int width, int height, int fps, int loop, int quality,
    Action<int, string> onFileSaved = null, Action<int, float> onFileSaveProgress = null, bool autoClear = true, bool destroyOriginTexture = false, bool smooth_yieldPerFrame = false)
    {
        filePath = SaveFolder + "/" + FileName + ".gif";
        StartCoroutine(_ISave(filePath, textureList, width, height, fps, loop, quality, onFileSaved, onFileSaveProgress, autoClear, destroyOriginTexture, smooth_yieldPerFrame));
        return filePath;
    }

    IEnumerator _ISave(string savePath, List<Texture2D> textureList, int width, int height, int fps, int loop, int quality,
        Action<int, string> onFileSaved = null, Action<int, float> onFileSaveProgress = null, bool autoClear = true, bool destroyOriginTexture = false, bool smooth_yieldPerFrame = false)
    {
        this.OnFileSaveProgress = onFileSaveProgress;
        this.OnFileSaved = onFileSaved;

        if (autoClear)
        {
            Action<int, string> clearCallback = (id, path) =>
            {
                Clear();
            };
            this.OnFileSaved += clearCallback;
        }

        // Process all the textures by target size and rotation
        m_PreviewTextures = new List<Texture2D>();
        List<Frame> frames = new List<Frame>();
        for (int i = 0; i < textureList.Count; i++)
        {
            frames.Add(Texture2DToFrame(textureList[i], width, height, destroyOriginTexture));
            if (smooth_yieldPerFrame) yield return null;
        }
        if (!smooth_yieldPerFrame) yield return null;

        StartEncode(frames, fps, loop, quality);
    }

    private Frame Texture2DToFrame(Texture2D texture2d, int width, int height, bool destroyOriginTexture)
    {
        if (texture2d.width != width || texture2d.height != height)
        {
            switch (m_ResolutionHandle)
            {
                case ResolutionHandle.Resize:
                    texture2d = _imageResizer.ResizeTexture32(texture2d, width, height, destroyOriginTexture);
                    break;
                case ResolutionHandle.ResizeKeepRatio:
                    texture2d = _imageResizer.ResizeTexture32_KeepRatio(texture2d, width, height, destroyOriginTexture);
                    break;
            }
        }
        m_PreviewTextures.Add(texture2d);

        //-- Rotate image (interval of 90 degrees) -----
        int newWidth = width;
        int newHeight = height;

        if (m_Rotation == ImageRotator.Rotation.None)
        {
            return new Frame() { Width = newWidth, Height = newHeight, Data = texture2d.GetPixels32() };
        }
        else
        {
            switch (m_Rotation)
            {
                case ImageRotator.Rotation.Right: //90
                    newWidth = height;
                    newHeight = width;
                    break;
                case ImageRotator.Rotation.HalfCircle: //180
                    break;
                case ImageRotator.Rotation.Left: //-90
                    newWidth = height;
                    newHeight = width;
                    break;
            }

            Color32[] colors = ImageRotator.RotateImageToColor32(texture2d, m_Rotation);
            return new Frame() { Width = newWidth, Height = newHeight, Data = colors };
        }
    }

    /// <summary>
    /// Convert and save a List of RenderTexture to GIF.
    /// </summary>
    /// <param name="textureList">Texture list.</param>
    /// <param name="width">Target width for the GIF.</param>
    /// <param name="height">Target height for thr GIF.</param>
    /// <param name="fps">Frame count per second.</param>
    /// <param name="loop">Repeat time, -1: no repeat, 0: infinite, >0: repeat count.</param>
    /// <param name="quality">Quality, (1 - 100) 1: best(larger storage size), 100: faster(smaller storage size)</param>
    /// <param name="onFileSaved">On file saved callback.</param>
    /// <param name="onFileSaveProgress">On file save progress callback.</param>
    /// <param name="resolutionHandle">The method to resize the textures if their size different to the target size.</param>
    /// <param name="autoClear">If set to <c>true</c>, clear this instance and textures when the GIF is saved.</param>
    /// <param name="destroyOriginTexture">If set to <c>true</c>, clear the original textures after processed.</param>
    /// <param name="smooth_yieldPerFrame">Process each texture in separate frame to avoid blocking the main thread, so the UI smoother.</param>
    public string Save(List<RenderTexture> textureList, int width, int height, int fps, int loop, int quality,
        Action<int, string> onFileSaved = null, Action<int, float> onFileSaveProgress = null,
        ResolutionHandle resolutionHandle = ResolutionHandle.ResizeKeepRatio, bool autoClear = true, bool destroyOriginTexture = false, bool smooth_yieldPerFrame = false)
    {
        this.m_ResolutionHandle = resolutionHandle;
        return _Save(textureList, width, height, fps, loop, quality, onFileSaved, onFileSaveProgress, autoClear, destroyOriginTexture, smooth_yieldPerFrame);
    }

    /// <summary>
    /// Convert and save a List of RenderTexture to GIF.
    /// </summary>
    /// <param name="textureList">Texture list.</param>
    /// <param name="width">Target width for the GIF.</param>
    /// <param name="height">Target height for thr GIF.</param>
    /// <param name="frameDelay">Frame delay time in seconds.</param>
    /// <param name="loop">Repeat time, -1: no repeat, 0: infinite, >0: repeat count.</param>
    /// <param name="quality">Quality, (1 - 100) 1: best(larger storage size), 100: faster(smaller storage size)</param>
    /// <param name="onFileSaved">On file saved callback.</param>
    /// <param name="onFileSaveProgress">On file save progress callback.</param>
    /// <param name="resolutionHandle">The method to resize the textures if their size different to the target size.</param>
    /// <param name="autoClear">If set to <c>true</c>, clear this instance and textures when the GIF is saved.</param>
    /// <param name="destroyOriginTexture">If set to <c>true</c>, clear the original textures after processed.</param>
    /// <param name="smooth_yieldPerFrame">Process each texture in separate frame to avoid blocking the main thread, so the UI smoother.</param>
    public string Save(List<RenderTexture> textureList, int width, int height, float frameDelay, int loop, int quality,
        Action<int, string> onFileSaved = null, Action<int, float> onFileSaveProgress = null,
        ResolutionHandle resolutionHandle = ResolutionHandle.ResizeKeepRatio, bool autoClear = true, bool destroyOriginTexture = false, bool smooth_yieldPerFrame = false)
    {
        frameDelay_Override = frameDelay;
        this.m_ResolutionHandle = resolutionHandle;
        return _Save(textureList, width, height, 0, loop, quality, onFileSaved, onFileSaveProgress, autoClear, destroyOriginTexture, smooth_yieldPerFrame);
    }

    private string _Save(List<RenderTexture> textureList, int width, int height, int fps, int loop, int quality,
        Action<int, string> onFileSaved = null, Action<int, float> onFileSaveProgress = null, bool autoClear = true, bool destroyOriginTexture = false, bool smooth_yieldPerFrame = false)
    {
        filePath = SaveFolder + "/" + FileName + ".gif";
        StartCoroutine(_ISave(filePath, textureList, width, height, fps, loop, quality, onFileSaved, onFileSaveProgress, autoClear, destroyOriginTexture, smooth_yieldPerFrame));
        return filePath;
    }

    private IEnumerator _ISave(string savePath, List<RenderTexture> textureList, int width, int height, int fps, int loop, int quality,
        Action<int, string> onFileSaved = null, Action<int, float> onFileSaveProgress = null, bool autoClear = true, bool destroyOriginTexture = false, bool smooth_yieldPerFrame = false)
    {
        this.OnFileSaveProgress = onFileSaveProgress;
        this.OnFileSaved = onFileSaved;

        if (autoClear)
        {
            Action<int, string> clearCallback = (id, path) =>
            {
                Clear();
            };
            this.OnFileSaved += clearCallback;
        }

        // Process all the textures by target size and rotation
        m_PreviewTextures = new List<Texture2D>();
        List<Frame> frames = new List<Frame>();
        for (int i = 0; i < textureList.Count; i++)
        {
            float texWidth = textureList[i].width;
            float texHeight = textureList[i].height;
            int newHeight = 0;

            switch (m_ResolutionHandle)
            {
                case ResolutionHandle.Resize:
                    newHeight = height;
                    break;
                case ResolutionHandle.ResizeKeepRatio:
                    float texRatio = texHeight / texWidth;
                    newHeight = Mathf.RoundToInt(width * texRatio);
                    break;
            }

            if (!texWidth.Equals(width) || !texHeight.Equals(newHeight))
            {
                RenderTexture newTex = new RenderTexture(width, newHeight, 24);
                Graphics.Blit(textureList[i], newTex);
                if (destroyOriginTexture) Flush(textureList[i]);
                textureList[i] = newTex;
            }

            // Get a temporary texture to read RenderTexture data
            Texture2D temp = new Texture2D(width, height, TextureFormat.ARGB32, false);
            temp.hideFlags = HideFlags.HideAndDontSave;
            temp.wrapMode = TextureWrapMode.Clamp;
            temp.filterMode = FilterMode.Bilinear;
            temp.anisoLevel = 0;

            frames.Add(RenderTextureToFrame(textureList[i], temp));
            if (smooth_yieldPerFrame) yield return null;
        }
        if (!smooth_yieldPerFrame) yield return null;

        StartEncode(frames, fps, loop, quality);
    }

    // Converts a RenderTexture to a GifFrame
    // Should be fast enough for low-res textures but will tank the framerate at higher res
    private Frame RenderTextureToFrame(RenderTexture source, Texture2D target)
    {
        RenderTexture.active = source;

        // Crop Image
        target.ReadPixels(new Rect((source.width - target.width) / 2, (source.height - target.height) / 2, target.width, target.height), 0, 0);
        //target.Apply();
        RenderTexture.active = null;

        m_PreviewTextures.Add(target);

        //-- Rotate Image (interval of 90 degrees) -----
        int newWidth = target.width;
        int newHeight = target.height;

        if (m_Rotation == ImageRotator.Rotation.None)
        {
            return new Frame() { Width = newWidth, Height = newHeight, Data = target.GetPixels32() };
        }
        else
        {
            switch (m_Rotation)
            {
                case ImageRotator.Rotation.Right: //90
                    newWidth = target.height;
                    newHeight = target.width;
                    break;
                case ImageRotator.Rotation.HalfCircle: //180
                    break;
                case ImageRotator.Rotation.Left: //-90
                    newWidth = target.height;
                    newHeight = target.width;
                    break;
            }

            Color32[] colors = ImageRotator.RotateImageToColor32(target, m_Rotation);
            return new Frame() { Width = newWidth, Height = newHeight, Data = colors };
        }
    }

    private void StartEncode(List<Frame> frames, int fps, int loop, int quality)
    {
        switch (m_EncodePlayMode)
        {
            case ProGifRecorderComponent.EncodePlayMode.Reverse:
                frames.Reverse();
                break;
            case ProGifRecorderComponent.EncodePlayMode.PingPong:
                int T = frames.Count;
                for (int i = 0; i < T; i++) frames.Add(frames[T - i - 1]);
                break;
        }

        frameCountFinal = frames.Count;

        if (_OnSaveFirstFrame != null) _OnSaveFirstFrame(frames[0].Data, frames[0].Width, frames[0].Height);

        // Multithreaded encoding starts here.
        numberOfThreads = Mathf.Min(frameCountFinal, m_MaxNumberOfThreads < 1 ? SystemInfo.processorCount : m_MaxNumberOfThreads);
#if UNITY_EDITOR
        Debug.Log("Number of threads: " + numberOfThreads);
#endif

        stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        framesFinished = 0;
        jobsFinished = 0;

        // Split frames
        workers = new List<ProGifWorker>();
        List<Frame>[] framesArray = new List<Frame>[numberOfThreads];

        int framesOnEachThread = Mathf.FloorToInt((float)frameCountFinal / (float)numberOfThreads);
        int leftOverFrames = frameCountFinal % numberOfThreads;

        int startIndex = 0;
        for (int threadIndex = 0; threadIndex < numberOfThreads; threadIndex++)
        {
            int leftOverFrameAvg = (leftOverFrames > 0 ? 1 : 0);
#if UNITY_EDITOR
            //Debug.Log($"ThreadIndex-{threadIndex}: {startIndex}-{startIndex + framesOnEachThread + leftOverFrameAvg - 1}, Total: {frameCountFinal}");
            Debug.Log("ThreadIndex-" + threadIndex + ": " + startIndex + "-" + (startIndex + framesOnEachThread + leftOverFrameAvg - 1) + ", Total: " + frameCountFinal);
#endif
            framesArray[threadIndex] = new List<Frame>();
            for (int i = startIndex; i < startIndex + framesOnEachThread + leftOverFrameAvg; i++)
            {
                framesArray[threadIndex].Add(frames[i]);
            }
            //The leftover frames are added to the first thread.
            startIndex += framesOnEachThread + leftOverFrameAvg;
            if (leftOverFrames > 0) leftOverFrames--;
        }

        for (int i = 0; i < numberOfThreads; i++)
        {
            // Setup a worker thread for GIF encoding and save file -----------------
            ProGifEncoder encoder = new ProGifEncoder(loop, quality, i, EncoderFinished);

            if (m_AutoTransparent)
            {
                encoder.m_AutoTransparent = m_AutoTransparent;
            }
            else if (m_TransparentColor.a != 0)
            {
                encoder.SetTransparencyColor(m_TransparentColor, m_TransparentColorRange);
            }

            // Check if apply the Override Frame Delay value
            if (frameDelay_Override > 0f)
            {
                encoder.SetDelay(Mathf.RoundToInt(frameDelay_Override * 1000f));
            }
            else
            {
                float timePerFrame = 1f / fps;
                encoder.SetDelay(Mathf.RoundToInt(timePerFrame * 1000f));
            }

            ProGifWorker worker = new ProGifWorker(m_WorkerPriority)
            {
                m_Encoder = encoder,
                m_Frames = framesArray[i],
                m_OnFileSaveProgress = FileSaveProgress
            };

            workers.Add(worker);

            // Make sure only the first encoder writes the beginning
            worker.m_Encoder.m_IsFirstFrame = i == 0;

            // Make sure only the last encoder appends the trail.
            worker.m_Encoder.m_IsLastEncoder = i == numberOfThreads - 1;

            worker.Start();
        }
    }

    // Flushe a single Texture object
    private void Flush(Texture texture)
    {
        if (RenderTexture.active == texture) return;

#if UNITY_EDITOR
        DestroyImmediate(texture);
#else
        Destroy(texture);
#endif
    }

    /// <summary>
    /// It is important to Clear textures every time (prevent memory leak)
    /// </summary>
    public void Clear()
    {
        _providedFileName = string.Empty;

        //Clear texture
        if (m_PreviewTextures != null)
        {
            foreach (Texture2D tex in m_PreviewTextures)
            {
                if (tex != null)
                {
                    Destroy(tex);
                }
            }
            m_PreviewTextures = null;
        }

        // Remove the dictionary reference
        _GifConverterDict.Remove(m_ConverterName);

        _instance = null;

        if (Application.isPlaying)
        {
            Destroy(gameObject);
        }
        else
        {
            DestroyImmediate(gameObject);
        }
    }


    #region ----- Others -----
    private List<string> _fileExtensions = new List<string> { ".jpg", ".png" };

    /// <summary>
    /// Set the file extensions filter for LoadImages.
    /// </summary>
    /// <param name="fileExtensions">File extension names in lower case</param>
    public void SetFileExtension(List<string> fileExtensions)
    {
        _fileExtensions = fileExtensions;
    }

    /// <summary>
    /// Load images from the target directory, to a texture2D list.
    /// </summary>
    /// <returns>The images.</returns>
    /// <param name="directory">Directory.</param>
    public List<Texture2D> LoadImages(string directory)
    {
        List<Texture2D> textureList = new List<Texture2D>();

        string[] allFiles_src = Directory.GetFiles(directory);
        foreach (string f in allFiles_src)
        {
            if (_fileExtensions.Contains(Path.GetExtension(f).ToLower()))
            {
                byte[] bytes = File.ReadAllBytes(f);

                Texture2D tex2D = new Texture2D(1, 1);
                tex2D.LoadImage(bytes);

                textureList.Add(tex2D);
            }
        }
        return textureList;
    }

    /// <summary>
    /// Load images from a resources floder, to a texture2D list. (eg.: Resources/Photo).
    /// </summary>
    /// <returns>The images from the resources floder.</returns>
    /// <param name="resourcesFolderPath">Resources folder path.</param>
    public List<Texture2D> LoadImagesFromResourcesFolder(string resourcesFolderPath = "Photo/")
    {
        //Load image as texture 2D from resources folder, do not support File Extension
        List<Texture2D> tex2DList = new List<Texture2D>();
        Texture2D[] tex2Ds = Resources.LoadAll<Texture2D>(resourcesFolderPath);
        if (tex2Ds != null && tex2Ds.Length > 0)
        {
            for (int i = 0; i < tex2Ds.Length; i++)
            {
                tex2DList.Add(tex2Ds[i]);
            }
        }
        return tex2DList;
    }

    public Sprite GetSprite(int index)
    {
        index = Mathf.Clamp(index, 0, m_PreviewTextures.Count - 1);
        return ToSprite(m_PreviewTextures[index]);
    }

    public Sprite ToSprite(Texture2D texture)
    {
        Vector2 pivot = new Vector2(0.5f, 0.5f);
        float pixelPerUnit = 100;
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), pivot, pixelPerUnit);
    }

    public void TakeScreenshot(Action<Texture2D> onComplete)
    {
        StartCoroutine(_TakeScreenshot(onComplete));
    }

    private IEnumerator _TakeScreenshot(Action<Texture2D> onComplete)
    {
        yield return new WaitForEndOfFrame();
        int width = Screen.width;
        int height = Screen.height;
        Texture2D readTex = new Texture2D(width, height, TextureFormat.RGB24, false);
        Rect rect = new Rect(0, 0, width, height);
        readTex.ReadPixels(rect, 0, 0);
        readTex.Apply();
        onComplete(readTex);
    }
    #endregion

}
