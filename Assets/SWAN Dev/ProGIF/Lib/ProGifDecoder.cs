// Created by SWAN DEV 2017

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pro GIF decoder ver 2.0
/// </summary>
public class ProGifDecoder
{
    #region ----- Global Variables -----
    public static bool SkipOptionalExtensions = false;
    public static LzwDecodeVersion LZW_Version = LzwDecodeVersion.V2;
    #endregion

    public enum LzwDecodeVersion
    {
        V1 = 0,
        V2,
    }

    private List<GifTexture> _tempGifTextures = null;

    //Decode settings
    private ProGifPlayerComponent.DecodeMode decodeMode = ProGifPlayerComponent.DecodeMode.Normal;
    private ProGifPlayerComponent.FramePickingMethod framePickingMethod = ProGifPlayerComponent.FramePickingMethod.ContinuousFromBeginning;
    private int targetDecodeFrameNum = -1;      //if targetDecodeFrameNum <= 0: decode & play all frames (+/- 1 frame)
    private float frameSkipNum = 0f;
    private bool optimizeMemoryUsgae = true;

    public void ResetDecodeSettings()
    {
        decodeMode = ProGifPlayerComponent.DecodeMode.Normal;
        framePickingMethod = ProGifPlayerComponent.FramePickingMethod.ContinuousFromBeginning;
        targetDecodeFrameNum = -1;
        frameSkipNum = 0f;
    }

    public void SetAdvancedDecodeSettings(int targetDecodeFrameNum = -1, ProGifPlayerComponent.FramePickingMethod framePickingMethod = ProGifPlayerComponent.FramePickingMethod.ContinuousFromBeginning)
    {
        this.decodeMode = ProGifPlayerComponent.DecodeMode.Advanced;
        this.framePickingMethod = framePickingMethod;
        this.targetDecodeFrameNum = targetDecodeFrameNum;
    }

    public void SetOptimizeMemoryUsgae(bool enable)
    {
        optimizeMemoryUsgae = enable;
    }

    #region ----- Constructor -----
    private bool _debugLog = false;
    private byte[] _gifBytes = null;
    private Action<List<GifTexture>, int, int, int> _onCompleteCallback = null;
    private Action<GifTexture> _onFrameReady = null;
    private Action<GifInfo> _getGifInfoCallback = null;
    private FilterMode _filterMode = FilterMode.Point;
    private TextureWrapMode _wrapMode = TextureWrapMode.Clamp;

    private byte[] _lastBlockData = null;

    public ProGifDecoder(byte[] bytes, Action<List<GifTexture>, int, int, int> onCompleteCallback,
        FilterMode filterMode = FilterMode.Bilinear, TextureWrapMode wrapMode = TextureWrapMode.Clamp,
        bool debugLog = false, Action<GifTexture> onFrameReady = null, Action<GifInfo> getGifInfoCallback = null)
    {
        _gifBytes = bytes;
        _onCompleteCallback = onCompleteCallback;
        _filterMode = filterMode;
        _wrapMode = wrapMode;
        _debugLog = debugLog;
        _onFrameReady = onFrameReady;
        _getGifInfoCallback = getGifInfoCallback;
    }
    #endregion //----- Constructor -----

    #region ----- Get GIF Info Without Texture -----
    public class GifInfo
    {
        public int m_ByteLength = 0;
        public int m_TotalFrame = 0;
        public int m_LoopCount = -1; // -1 = no loop, 0 = loop
        public int m_Width = 0;
        public int m_Height = 0;
        public float m_FPS = 0f;
        public float m_DelaySec = 0f;
    }

    public ProGifDecoder() { }

    public GifInfo GetGifInfo(byte[] bytes)
    {
        // Set GIF data
        GifData gifData = new GifData();
        if (SetGifData(bytes, ref gifData, _debugLog) == false)
        {
            Debug.LogWarning("GIF file data set error.");
            return null;
        }

        GifInfo info = new GifInfo();
        info.m_TotalFrame = gifData.m_imageBlockList.Count;
        GraphicControlExtension? graphicCtrlEx = GetGraphicCtrlExt(gifData, 0);
        info.m_DelaySec = GetDelaySec(graphicCtrlEx);
        info.m_FPS = 1f / info.m_DelaySec;
        info.m_LoopCount = gifData.m_appEx.loopCount;
        info.m_Width = gifData.m_logicalScreenWidth;
        info.m_Height = gifData.m_logicalScreenHeight;
        return info;
    }
    #endregion

    #region ----- Decode Run-In-Coroutine -----
    /// <summary> Decode the gif data and set GifTexture list (Coroutine). </summary>
    public IEnumerator GetTextureListCoroutine()
    {
        int loopCount = -1;
        int width = 0;
        int height = 0;

        // Set GIF data
        GifData gifData = new GifData();

        if (SetGifData(_gifBytes, ref gifData, _debugLog) == false)
        {
            Debug.LogWarning("GIF file data set error.");
            if (_onCompleteCallback != null)
            {
                _onCompleteCallback(null, loopCount, width, height);
            }
            yield break;
        }

        int totalFrame = gifData.m_imageBlockList.Count;
        int byteLength = _gifBytes.Length;
        loopCount = gifData.m_appEx.loopCount;
        width = gifData.m_logicalScreenWidth;
        height = gifData.m_logicalScreenHeight;

        if (_getGifInfoCallback != null)
        {
            GifInfo info = new GifInfo();
            info.m_TotalFrame = totalFrame;
            GraphicControlExtension? graphicCtrlEx = GetGraphicCtrlExt(gifData, 0);
            info.m_DelaySec = GetDelaySec(graphicCtrlEx);
            info.m_FPS = 1f / info.m_DelaySec;
            info.m_LoopCount = loopCount;
            info.m_Width = width;
            info.m_Height = height;

            _getGifInfoCallback(info);
        }

        // Decode to textures from GIF data
        yield return DecodeTextureCoroutine(gifData, (result) => { _tempGifTextures = result; }, _onFrameReady);

        if (_tempGifTextures == null || _tempGifTextures.Count <= 0)
        {
            Debug.LogWarning("GIF texture decode error.");
            if (_onCompleteCallback != null)
            {
                _onCompleteCallback(null, loopCount, width, height);
            }
            yield break;
        }

        if (_onCompleteCallback != null)
        {
            _onCompleteCallback(_tempGifTextures, loopCount, width, height);
        }

        yield break;
    }

    /// <summary> Decode the gif data and set GifTexture list (Coroutine). </summary>
    private IEnumerator DecodeTextureCoroutine(GifData gifData, Action<List<GifTexture>> callback, Action<GifTexture> onFrameReady = null)
    {
        if (gifData.m_imageBlockList == null || gifData.m_imageBlockList.Count < 1)
        {
            yield break;
        }

        List<GifTexture> gifTexList = new List<GifTexture>();

        // Disposal Method
        // 0 (No disposal specified)
        // 1 (Do not dispose)
        // 2 (Restore to background color / no overlapping)
        // 3 (Restore to previous block)
        List<ushort> disposalMethodList = new List<ushort>(gifData.m_imageBlockList.Count);

        int decodeIndex = 0;

        List<int> decodeFrames = _GetDecodeFrames(gifData);

        for (int i = 0; i < gifData.m_imageBlockList.Count; i++)
        {
            bool doDecode = true;

            //-- Advanced mode ------
            if (decodeMode == ProGifPlayerComponent.DecodeMode.Advanced)
            {
                doDecode = (decodeFrames.Contains(i));
            }
            //-- Advanced mode ------

            if (doDecode)
            {
                byte[] decodedData = GetDecodedData(gifData.m_imageBlockList[i]);

                GraphicControlExtension? graphicCtrlEx = GetGraphicCtrlExt(gifData, i); // decodeIndex);

                int transparentIndex = GetTransparentIndex(graphicCtrlEx);

                Color32 bgColor;
                List<byte[]> colorTable = GetColorTable(gifData, gifData.m_imageBlockList[i], transparentIndex, out bgColor);

                yield return null;

                disposalMethodList.Add(GetDisposalMethod(graphicCtrlEx));

                Color32[] colors = null;
                colors = SetPixel(gifData, gifTexList, decodeIndex, disposalMethodList, i, decodedData, colorTable, bgColor, transparentIndex);

                yield return null;

                float delaySec = GetDelaySec(graphicCtrlEx);

                ////Advanced mode
                //if (decodeMode == ProGifPlayerComponent.DecodeMode.Advanced && frameSkipNum > 0)
                //{
                //    //Retain the origin playing speed
                //    delaySec *= frameSkipNum;
                //}

                // Add to GIF texture list
                GifTexture gTex = new GifTexture(colors, gifData.m_logicalScreenWidth, gifData.m_logicalScreenHeight, delaySec, _filterMode, _wrapMode, optimizeMemoryUsgae);
                gifTexList.Add(gTex);
                if (onFrameReady != null)
                {
                    onFrameReady(gTex);
                }

                decodeIndex++;
            }
        }

        if (callback != null)
        {
            callback(gifTexList);
        }

        yield break;
    }
    #endregion //----- Decode Run-In-Coroutine -----

    private List<int> _GetDecodeFrames(GifData gifData)
    {
        List<int> decodeFrames = new List<int>();
        if (decodeMode == ProGifPlayerComponent.DecodeMode.Advanced)
        {
            switch (framePickingMethod)
            {
                case ProGifPlayerComponent.FramePickingMethod.OneHalf:
                    int divided2 = Mathf.Max(2, gifData.m_imageBlockList.Count / 2);
                    targetDecodeFrameNum = (targetDecodeFrameNum > 0) ? Mathf.Min(targetDecodeFrameNum, divided2) : divided2;
                    break;
                case ProGifPlayerComponent.FramePickingMethod.OneThird:
                    int divided3 = Mathf.Max(2, gifData.m_imageBlockList.Count / 3);
                    targetDecodeFrameNum = (targetDecodeFrameNum > 0) ? Mathf.Min(targetDecodeFrameNum, divided3) : divided3;
                    break;
                case ProGifPlayerComponent.FramePickingMethod.OneFourth:
                    int divided4 = Mathf.Max(2, gifData.m_imageBlockList.Count / 4);
                    targetDecodeFrameNum = (targetDecodeFrameNum > 0) ? Mathf.Min(targetDecodeFrameNum, divided4) : divided4;
                    break;
            }

            if (targetDecodeFrameNum > gifData.m_imageBlockList.Count || targetDecodeFrameNum <= 0) targetDecodeFrameNum = gifData.m_imageBlockList.Count;
            frameSkipNum = (targetDecodeFrameNum > 0) ? (float)gifData.m_imageBlockList.Count / (float)(targetDecodeFrameNum) : 0;

            float decodeFrameIndex = 0f;
            for (int i = 0; i < targetDecodeFrameNum; i++)
            {
                if ((int)decodeFrameIndex < gifData.m_imageBlockList.Count) decodeFrames.Add((int)decodeFrameIndex);

                switch (framePickingMethod)
                {
                    case ProGifPlayerComponent.FramePickingMethod.OneHalf:
                    case ProGifPlayerComponent.FramePickingMethod.OneThird:
                    case ProGifPlayerComponent.FramePickingMethod.OneFourth:
                    case ProGifPlayerComponent.FramePickingMethod.ContinuousFromBeginning:
                        decodeFrameIndex++;
                        break;
                    case ProGifPlayerComponent.FramePickingMethod.AverageInterval:
                        decodeFrameIndex += frameSkipNum;
                        break;
                }
            }
        }

        return decodeFrames;
    }

    #region ----- Decode Run-In-Threads -----
    internal bool threadAborted = false;
    internal bool runningInThread = false;
    private int _decoderIndex = -1;
    private Action<int> _workerCallback = null;
    /// <summary> Start method of the decoder worker thread, called in ProGifDeWorker. (Threads) </summary>
    internal void StartDecode(int decoderIndex, Action<int> workerCallback)
    {
        _decoderIndex = decoderIndex;
        _workerCallback = workerCallback;
        threadAborted = false;
        runningInThread = true;

        int loopCount = -1;
        int width = 0;
        int height = 0;

        // Set GIF data
        GifData gifData = new GifData();

        if (SetGifData(_gifBytes, ref gifData, _debugLog) == false)
        {
            Debug.LogWarning("GIF file data set error.");
            if (_onCompleteCallback != null)
            {
                _onCompleteCallback(null, loopCount, width, height);
            }
            return;
        }

        int totalFrame = gifData.m_imageBlockList.Count;
        int byteLength = _gifBytes.Length;
        loopCount = gifData.m_appEx.loopCount;
        width = gifData.m_logicalScreenWidth;
        height = gifData.m_logicalScreenHeight;

        if (_getGifInfoCallback != null)
        {
            GifInfo info = new GifInfo();
            info.m_TotalFrame = totalFrame;
            GraphicControlExtension? graphicCtrlEx = GetGraphicCtrlExt(gifData, 0);
            info.m_DelaySec = GetDelaySec(graphicCtrlEx);
            info.m_FPS = 1f / info.m_DelaySec;
            info.m_LoopCount = loopCount;
            info.m_Width = width;
            info.m_Height = height;

            _getGifInfoCallback(info);
        }

        // Decode to GifTextures from GIF data
        DecodeTexture(gifData,
            (result) => {
                _tempGifTextures = result;
            },
            (gTex) => {
                if (!threadAborted) _onFrameReady(gTex);
            }
        );

        if (threadAborted)
        {
            ThreadClear();
            return;
        }

        if (_tempGifTextures == null || _tempGifTextures.Count <= 0)
        {
            Debug.LogWarning("GIF texture decode error.");
            if (_onCompleteCallback != null)
            {
                _onCompleteCallback(null, 0, 0, 0);
            }
            return;
        }

        if (_onCompleteCallback != null)
        {
            _onCompleteCallback(_tempGifTextures, loopCount, width, height);
        }
        _workerCallback(decoderIndex);
        _workerCallback = null;
        runningInThread = false;
    }

    internal void ThreadClear()
    {
        threadAborted = true;
        runningInThread = false;

        if (_onCompleteCallback != null)
        {
            _onCompleteCallback(null, 0, 0, 0);
            _onCompleteCallback = null;
        }

        if (_workerCallback != null)
        {
            _workerCallback(_decoderIndex);
            _workerCallback = null;
        }

        ClearGifTextures();
    }

    /// <summary> Decode the gif data and set GifTexture list (Threads). </summary>
    private void DecodeTexture(GifData gifData, Action<List<GifTexture>> callback, Action<GifTexture> onFrameReady = null)
    {
        if (gifData.m_imageBlockList == null || gifData.m_imageBlockList.Count < 1)
        {
            return;
        }

        List<GifTexture> gifTexList = new List<GifTexture>();

        // Disposal Method
        // 0 (No disposal specified)
        // 1 (Do not dispose)
        // 2 (Restore to background color / no overlapping)
        // 3 (Restore to previous block)
        List<ushort> disposalMethodList = new List<ushort>(gifData.m_imageBlockList.Count);

        int decodeIndex = 0;

        List<int> decodeFrames = _GetDecodeFrames(gifData);

        for (int i = 0; i < gifData.m_imageBlockList.Count; i++)
        {
            if (threadAborted) break;

            bool doDecode = true;

            //-- Advanced mode ------
            if (decodeMode == ProGifPlayerComponent.DecodeMode.Advanced)
            {
                doDecode = (decodeFrames.Contains(i));
            }
            //-- Advanced mode ------

            if (doDecode)
            {
                byte[] decodedData = GetDecodedData(gifData.m_imageBlockList[i]);

                GraphicControlExtension? graphicCtrlEx = GetGraphicCtrlExt(gifData, i); // decodeIndex);

                int transparentIndex = GetTransparentIndex(graphicCtrlEx);

                Color32 bgColor;
                List<byte[]> colorTable = GetColorTable(gifData, gifData.m_imageBlockList[i], transparentIndex, out bgColor);

                disposalMethodList.Add(GetDisposalMethod(graphicCtrlEx));

                Color32[] colors = null;
                
                colors = SetPixel(gifData, gifTexList, decodeIndex, disposalMethodList, i, decodedData, colorTable, bgColor, transparentIndex);

                float delaySec = GetDelaySec(graphicCtrlEx);

                ////Advanced mode
                //if (decodeMode == ProGifPlayerComponent.DecodeMode.Advanced && frameSkipNum > 0)
                //{
                //    //Retain the origin playing speed
                //    delaySec *= frameSkipNum;
                //}

                if (threadAborted) break;

                // Add to GIF texture list
                GifTexture gTex = new GifTexture(colors, gifData.m_logicalScreenWidth, gifData.m_logicalScreenHeight, delaySec, _filterMode, _wrapMode, optimizeMemoryUsgae);
                gifTexList.Add(gTex);
                if (onFrameReady != null)
                {
                    onFrameReady(gTex);
                }

                decodeIndex++;
            }
        }

        if (callback != null)
        {
            callback(gifTexList);
        }
    }
    #endregion //----- Decode Run-In-Threads -----

    /// <summary>
    /// GIF Data Format
    /// </summary>
    internal struct GifData
    {
        // Signature
        public byte m_sig0, m_sig1, m_sig2;
        // Version
        public byte m_ver0, m_ver1, m_ver2;
        // Logical Screen Width
        public ushort m_logicalScreenWidth;
        // Logical Screen Height
        public ushort m_logicalScreenHeight;
        // Global Color Table Flag
        public bool m_globalColorTableFlag;
        // Color Resolution
        public int m_colorResolution;
        // Sort Flag
        public bool m_sortFlag;
        // Size of Global Color Table
        public int m_sizeOfGlobalColorTable;
        // Background Color Index
        public byte m_bgColorIndex;
        // Pixel Aspect Ratio
        public byte m_pixelAspectRatio;
        // Global Color Table
        public List<byte[]> m_globalColorTable;
        // ImageBlock
        public List<ImageBlock> m_imageBlockList;
        // GraphicControlExtension
        public List<GraphicControlExtension> m_graphicCtrlExList;
        // Comment Extension
        public List<CommentExtension> m_commentExList;
        // Plain Text Extension
        public List<PlainTextExtension> m_plainTextExList;
        // Application Extension
        public ApplicationExtension m_appEx;
        // Trailer
        public byte m_trailer;

        public string signature
        {
            get
            {
                char[] c = { (char)m_sig0, (char)m_sig1, (char)m_sig2 };
                return new string(c);
            }
        }

        public string version
        {
            get
            {
                char[] c = { (char)m_ver0, (char)m_ver1, (char)m_ver2 };
                return new string(c);
            }
        }

        public void Dump()
        {
            Debug.Log("GIF Type: " + signature + "-" + version);
            Debug.Log("Image Size: " + m_logicalScreenWidth + "x" + m_logicalScreenHeight);
            Debug.Log("Animation Image Count: " + m_imageBlockList.Count);
            Debug.Log("Animation Loop Count (0 is infinite): " + m_appEx.loopCount);
            if (m_graphicCtrlExList != null && m_graphicCtrlExList.Count > 0)
            {
                var sb = new StringBuilder("Animation Delay Time (1/100sec)");
                for (int i = 0; i < m_graphicCtrlExList.Count; i++)
                {
                    sb.Append(", ");
                    sb.Append(m_graphicCtrlExList[i].m_delayTime);
                }
                Debug.Log(sb.ToString());
            }
            Debug.Log("Application Identifier: " + m_appEx.applicationIdentifier);
            Debug.Log("Application Authentication Code: " + m_appEx.applicationAuthenticationCode);

            if (m_appEx.m_appDataList != null)
            {
                Debug.Log("Application DataList Count: " + m_appEx.m_appDataList.Count);
                for (int i = 0; i < m_appEx.m_appDataList.Count; i++)
                {
                    byte[] meta = m_appEx.m_appDataList[i].m_applicationData;
                    string s = System.Text.Encoding.UTF8.GetString(meta, 0, meta.Length);
                    Debug.Log(" > " + i + " block size: " + m_appEx.m_appDataList[i].m_blockSize + "\n" + s.ToString());
                }
            }

            if (m_commentExList != null)
            {
                for (int i = 0; i < m_commentExList.Count; i++)
                {
                    string s = "Comment Extension - " + i + " DataList Count: " + m_commentExList[i].m_commentDataList.Count + " Comment Label: " + m_commentExList[i].m_commentLabel + "\n";
                    for (int k = 0; k < m_commentExList[i].m_commentDataList.Count; k++)
                    {
                        s += " > " + k + ": " + m_commentExList[i].m_commentDataList[k].GetComment() + "\n";
                    }
                    Debug.Log(s.ToString());
                }
            }

            if (m_plainTextExList != null)
            {
                for (int i = 0; i < m_plainTextExList.Count; i++)
                {
                    string s = "PlainText Extension - " + i + " DataList Count: " + m_plainTextExList[i].m_plainTextDataList.Count + "\n";

                    for (int k = 0; k < m_plainTextExList[i].m_plainTextDataList.Count; k++)
                    {
                        byte[] meta = m_plainTextExList[i].m_plainTextDataList[k].m_plainTextData;
                        s += System.Text.Encoding.UTF8.GetString(meta, 0, meta.Length) + "\n";
                    }
                    Debug.Log(" > " + i + " block size: " + m_plainTextExList[i].m_blockSize + "\n" + s.ToString());
                }
            }
        }
    }

    /// <summary>
    /// Image Block
    /// </summary>
    internal struct ImageBlock
    {
        // Image Separator
        public byte m_imageSeparator;
        // Image Left Position
        public ushort m_imageLeftPosition;
        // Image Top Position
        public ushort m_imageTopPosition;
        // Image Width
        public ushort m_imageWidth;
        // Image Height
        public ushort m_imageHeight;
        // Local Color Table Flag
        public bool m_localColorTableFlag;
        // Interlace Flag
        public bool m_interlaceFlag;
        // Sort Flag
        public bool m_sortFlag;
        // Size of Local Color Table
        public int m_sizeOfLocalColorTable;
        // Local Color Table
        public List<byte[]> m_localColorTable;
        // LZW Minimum Code Size
        public byte m_lzwMinimumCodeSize;
        // Block Size & Image Data List
        public List<ImageDataBlock> m_imageDataList;

        public struct ImageDataBlock
        {
            // Block Size
            public byte m_blockSize;
            // Image Data
            public byte[] m_imageData;
        }
    }

    /// <summary>
    /// Graphic Control Extension
    /// </summary>
    internal struct GraphicControlExtension
    {
        // Extension Introducer
        public byte m_extensionIntroducer;
        // Graphic Control Label
        public byte m_graphicControlLabel;
        // Block Size
        public byte m_blockSize;
        // Disposal Mothod
        public ushort m_disposalMethod;
        // Transparent Color Flag
        public bool m_transparentColorFlag;
        // Delay Time
        public ushort m_delayTime;
        // Transparent Color Index
        public byte m_transparentColorIndex;
        // Block Terminator
        public byte m_blockTerminator;
    }

    /// <summary>
    /// Comment Extension
    /// </summary>
    internal struct CommentExtension
    {
        // Extension Introducer
        public byte m_extensionIntroducer;
        // Comment Label
        public byte m_commentLabel;
        // Block Size & Comment Data List
        public List<CommentDataBlock> m_commentDataList;

        public struct CommentDataBlock
        {
            // Block Size
            public byte m_blockSize;
            // Image Data
            public byte[] m_commentData;

            public string GetComment()
            {
                return (m_commentData == null ? "" : System.Text.Encoding.UTF8.GetString(m_commentData, 0, m_commentData.Length));
            }
        }
    }

    /// <summary>
    /// Plain Text Extension
    /// </summary>
    internal struct PlainTextExtension
    {
        // Extension Introducer
        public byte m_extensionIntroducer;
        // Plain Text Label
        public byte m_plainTextLabel;
        // Block Size
        public byte m_blockSize;
        // Block Size & Plain Text Data List
        public List<PlainTextDataBlock> m_plainTextDataList;

        public struct PlainTextDataBlock
        {
            // Block Size
            public byte m_blockSize;
            // Plain Text Data
            public byte[] m_plainTextData;
        }
    }

    /// <summary>
    /// Application Extension
    /// </summary>
    internal struct ApplicationExtension
    {
        // Extension Introducer
        public byte m_extensionIntroducer;
        // Extension Label
        public byte m_extensionLabel;
        // Block Size
        public byte m_blockSize;
        // Application Identifier
        public byte m_appId1, m_appId2, m_appId3, m_appId4, m_appId5, m_appId6, m_appId7, m_appId8;
        // Application Authentication Code
        public byte m_appAuthCode1, m_appAuthCode2, m_appAuthCode3;
        // Block Size & Application Data List
        public List<ApplicationDataBlock> m_appDataList;

        public struct ApplicationDataBlock
        {
            // Block Size
            public byte m_blockSize;
            // Application Data
            public byte[] m_applicationData;
        }

        public string applicationIdentifier
        {
            get
            {
                char[] c = { (char)m_appId1, (char)m_appId2, (char)m_appId3, (char)m_appId4, (char)m_appId5, (char)m_appId6, (char)m_appId7, (char)m_appId8 };
                return new string(c);
            }
        }

        public string applicationAuthenticationCode
        {
            get
            {
                char[] c = { (char)m_appAuthCode1, (char)m_appAuthCode2, (char)m_appAuthCode3 };
                return new string(c);
            }
        }

        public int loopCount
        {
            get
            {
                if (m_appDataList == null || m_appDataList.Count < 1 ||
                    m_appDataList[0].m_applicationData.Length < 3 ||
                    m_appDataList[0].m_applicationData[0] != 0x01)
                {
                    return 0;
                }
                return BitConverter.ToUInt16(m_appDataList[0].m_applicationData, 1);
            }
        }
    }


    /// <summary>
    /// Get background color from global color table
    /// </summary>
    private Color32? GetGlobalBgColor(GifData gifData)
    {
        Color32? bgColor = null;
        if (gifData.m_globalColorTableFlag)
        {
            // Set background color from global color table
            byte[] bgRgb = gifData.m_globalColorTable[gifData.m_bgColorIndex];
            bgColor = new Color32(bgRgb[0], bgRgb[1], bgRgb[2], 255);
        }
        return bgColor;
    }

    //private int _lastBlockImageWidth = 0;
    //private int _lastBlockImageHeight = 0;
    /// <summary>
    /// Get decoded image data from ImageBlock
    /// </summary>
    private byte[] GetDecodedData(ImageBlock imgBlock)
    {
        // Combine LZW compressed data
        int len = 0;
        for (int i = 0; i < imgBlock.m_imageDataList.Count; i++)
        {
            len += imgBlock.m_imageDataList[i].m_imageData.Length;
        }
        byte[] lzwData = new byte[len];
        int idx = 0;
        for (int i = 0; i < imgBlock.m_imageDataList.Count; i++)
        {
            for (int k = 0; k < imgBlock.m_imageDataList[i].m_imageData.Length; k++)
            {
                lzwData[idx++] = imgBlock.m_imageDataList[i].m_imageData[k];
            }
        }
        // Combine LZW compressed data

        // LZW decode
        int needDataSize = imgBlock.m_imageHeight * imgBlock.m_imageWidth;

        byte[] decodedData;
        switch (LZW_Version)
        {
            default:
            case LzwDecodeVersion.V1:
                decodedData = LzwDecodeV1(lzwData, imgBlock.m_lzwMinimumCodeSize, needDataSize);
                break;
            case LzwDecodeVersion.V2:
                decodedData = LzwDecodeV2(lzwData, imgBlock.m_lzwMinimumCodeSize, needDataSize);
                break;
        }

        return decodedData;
    }

    /// <summary>
    /// Get color table and set background color (local or global)
    /// </summary>
    private List<byte[]> GetColorTable(GifData gifData, ImageBlock imgBlock, int transparentIndex, out Color32 bgColor)
    {
        List<byte[]> colorTable = imgBlock.m_localColorTableFlag ? imgBlock.m_localColorTable : gifData.m_globalColorTable;

        if (_debugLog)
        {
            if (gifData.m_globalColorTableFlag) Debug.Log("Has Global color table. Size: " + gifData.m_globalColorTable.Count);
            if (imgBlock.m_localColorTableFlag) Debug.Log("Has Local color table. Size: " + imgBlock.m_localColorTable.Count);
        }

        if (colorTable != null)
        {
            // Get background color from color table
            if (gifData.m_bgColorIndex < colorTable.Count)
            {
                byte[] bgRgb = colorTable[gifData.m_bgColorIndex];
                bgColor = new Color32(bgRgb[0], bgRgb[1], bgRgb[2], (byte)(transparentIndex == gifData.m_bgColorIndex ? 0 : 255));
            }
            else
            {
                bgColor = new Color32(0, 0, 0, 0);
            }
        }
        else
        {
            if (_debugLog) Debug.Log("No Global or Local color table.");
            bgColor = Color.black;
        }

        return colorTable;
    }

    /// <summary>
    /// Get GraphicControlExtension from GifData
    /// </summary>
    private GraphicControlExtension? GetGraphicCtrlExt(GifData gifData, int imgBlockIndex)
    {
        if (gifData.m_graphicCtrlExList != null && gifData.m_graphicCtrlExList.Count > imgBlockIndex)
        {
            return gifData.m_graphicCtrlExList[imgBlockIndex];
        }
        return null;
    }

    /// <summary>
    /// Get transparent color index from GraphicControlExtension
    /// </summary>
    private int GetTransparentIndex(GraphicControlExtension? graphicCtrlEx)
    {
        int transparentIndex = -1;
        if (graphicCtrlEx != null && graphicCtrlEx.Value.m_transparentColorFlag)
        {
            transparentIndex = graphicCtrlEx.Value.m_transparentColorIndex;
        }
        return transparentIndex;
    }

    /// <summary>
    /// Get delay seconds from GraphicControlExtension
    /// </summary>
    private float GetDelaySec(GraphicControlExtension? graphicCtrlEx)
    {
        // Get delay sec from GraphicControlExtension
        float delaySec = graphicCtrlEx != null ? graphicCtrlEx.Value.m_delayTime / 100f : (1f / 60f);
        if (delaySec <= 0f)
        {
            delaySec = 0.1f;
        }
        return delaySec;
    }

    /// <summary>
    /// Get disposal method from GraphicControlExtension
    /// </summary>
    private ushort GetDisposalMethod(GraphicControlExtension? graphicCtrlEx)
    {
        if (_debugLog && graphicCtrlEx != null) Debug.Log("Get Disposal Method: " + graphicCtrlEx.Value.m_disposalMethod);
        return graphicCtrlEx != null ? graphicCtrlEx.Value.m_disposalMethod : (ushort)2;
    }

    /// <summary>
    /// Set GIF data
    /// </summary>
    /// <param name="gifBytes">GIF byte data</param>
    /// <param name="gifData">ref GIF data</param>
    /// <param name="debugLog">Debug log flag</param>
    /// <returns>Result</returns>
    private bool SetGifData(byte[] gifBytes, ref GifData gifData, bool debugLog)
    {
        if (debugLog)
        {
            Debug.Log("SetGifData Start.");
        }

        if (gifBytes == null || gifBytes.Length <= 0)
        {
            Debug.LogWarning("bytes is nothing.");
            return false;
        }

        int byteIndex = 0;

        if (SetGifHeader(gifBytes, ref byteIndex, ref gifData) == false)
        {
            Debug.LogWarning("GIF header set error.");
            return false;
        }

        if (SetGifBlock(gifBytes, ref byteIndex, ref gifData) == false)
        {
            Debug.LogWarning("GIF block set error.");
            return false;
        }

        if (debugLog)
        {
            gifData.Dump();
            Debug.Log("SetGifData Finish.");
        }
        return true;
    }

    private bool SetGifHeader(byte[] gifBytes, ref int byteIndex, ref GifData gifData)
    {
        // Signature(3 Bytes)
        // 0x47 0x49 0x46 (GIF)
        if (gifBytes[0] != 'G' || gifBytes[1] != 'I' || gifBytes[2] != 'F')
        {
            Debug.LogWarning("This is not GIF image.");
            return false;
        }
        gifData.m_sig0 = gifBytes[0];
        gifData.m_sig1 = gifBytes[1];
        gifData.m_sig2 = gifBytes[2];

        // Version(3 Bytes)
        // 0x38 0x37 0x61 (87a) or 0x38 0x39 0x61 (89a)
        if ((gifBytes[3] != '8' || gifBytes[4] != '7' || gifBytes[5] != 'a') &&
            (gifBytes[3] != '8' || gifBytes[4] != '9' || gifBytes[5] != 'a'))
        {
            Debug.LogWarning("GIF version error.\nSupported only GIF87a or GIF89a.");
            return false;
        }
        gifData.m_ver0 = gifBytes[3];
        gifData.m_ver1 = gifBytes[4];
        gifData.m_ver2 = gifBytes[5];

        // Logical Screen Width(2 Bytes)
        gifData.m_logicalScreenWidth = BitConverter.ToUInt16(gifBytes, 6);

        // Logical Screen Height(2 Bytes)
        gifData.m_logicalScreenHeight = BitConverter.ToUInt16(gifBytes, 8);

        // 1 Byte
        {
            // Global Color Table Flag(1 Bit)
            gifData.m_globalColorTableFlag = (gifBytes[10] & 128) == 128; // 0b10000000

            // Color Resolution(3 Bits)
            switch (gifBytes[10] & 112)
            {
                case 112:// 0b01110000
                    gifData.m_colorResolution = 8;
                    break;
                case 96: // 0b01100000
                    gifData.m_colorResolution = 7;
                    break;
                case 80: // 0b01010000
                    gifData.m_colorResolution = 6;
                    break;
                case 64: // 0b01000000
                    gifData.m_colorResolution = 5;
                    break;
                case 48: // 0b00110000
                    gifData.m_colorResolution = 4;
                    break;
                case 32: // 0b00100000
                    gifData.m_colorResolution = 3;
                    break;
                case 16: // 0b00010000
                    gifData.m_colorResolution = 2;
                    break;
                default:
                    gifData.m_colorResolution = 1;
                    break;
            }

            // Sort Flag(1 Bit)
            gifData.m_sortFlag = (gifBytes[10] & 8) == 8; // 0b00001000

            // Size of Global Color Table(3 Bits)
            int val = (gifBytes[10] & 7) + 1;
            gifData.m_sizeOfGlobalColorTable = (int)Math.Pow(2, val);
        }

        // Background Color Index(1 Byte)
        gifData.m_bgColorIndex = gifBytes[11];

        // Pixel Aspect Ratio(1 Byte)
        gifData.m_pixelAspectRatio = gifBytes[12];

        byteIndex = 13;
        if (gifData.m_globalColorTableFlag)
        {
            // Global Color Table(0～255×3 Bytes)
            gifData.m_globalColorTable = new List<byte[]>();
            for (int i = byteIndex; i < byteIndex + (gifData.m_sizeOfGlobalColorTable * 3); i += 3)
            {
                gifData.m_globalColorTable.Add(new byte[] { gifBytes[i], gifBytes[i + 1], gifBytes[i + 2] });
            }
            byteIndex = byteIndex + (gifData.m_sizeOfGlobalColorTable * 3);
        }

        return true;
    }

    private bool SetGifBlock(byte[] gifBytes, ref int byteIndex, ref GifData gifData)
    {
        try
        {
            int lastIndex = 0;
            while (true)
            {
                int nowIndex = byteIndex;

                if (gifBytes[nowIndex] == 0x2c)
                {
                    // Image Block(0x2c)
                    SetImageBlock(gifBytes, ref byteIndex, ref gifData);
                }
                else if (gifBytes[nowIndex] == 0x21)
                {
                    // Extension
                    switch (gifBytes[nowIndex + 1])
                    {
                        case 0xf9:
                            // Graphic Control Extension(0x21 0xf9)
                            SetGraphicControlExtension(gifBytes, ref byteIndex, ref gifData);
                            break;
                        case 0xfe:
                            // Comment Extension(0x21 0xfe)
                            SetCommentExtension(gifBytes, ref byteIndex, ref gifData);
                            break;
                        case 0x01:
                            // Plain Text Extension(0x21 0x01)
                            SetPlainTextExtension(gifBytes, ref byteIndex, ref gifData);
                            break;
                        case 0xff:
                            // Application Extension(0x21 0xff)
                            SetApplicationExtension(gifBytes, ref byteIndex, ref gifData);
                            break;
                        default:
                            Debug.LogWarning("Un-Interesting extension!! byteIndex = " + byteIndex);
                            //byteIndex++; // DEBUG
                            break;
                    }
                }
                else if (gifBytes[nowIndex] == 0x3b)
                {
                    // Trailer(1 Byte)
                    gifData.m_trailer = gifBytes[byteIndex];
                    byteIndex++;
                    break;
                }
                else byteIndex++; //Fix SetCommentExtension (2019-04-13)

                if (lastIndex == nowIndex)
                {
                    Debug.LogWarning("Infinite loop error.");
                    return false;
                }

                lastIndex = nowIndex;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("(SetGifBlock) " + ex.Message + "\n byteIndex: " + byteIndex);
            return false;
        }

        return true;
    }

    private void SetImageBlock(byte[] gifBytes, ref int byteIndex, ref GifData gifData)
    {
        ImageBlock ib = new ImageBlock();

        // Image Separator(1 Byte)
        // 0x2c
        ib.m_imageSeparator = gifBytes[byteIndex];
        byteIndex++;

        // Image Left Position(2 Bytes)
        ib.m_imageLeftPosition = BitConverter.ToUInt16(gifBytes, byteIndex);
        byteIndex += 2;

        // Image Top Position(2 Bytes)
        ib.m_imageTopPosition = BitConverter.ToUInt16(gifBytes, byteIndex);
        byteIndex += 2;

        // Image Width(2 Bytes)
        ib.m_imageWidth = BitConverter.ToUInt16(gifBytes, byteIndex);
        //if (ib.m_imageWidth > gifData.m_logicalScreenWidth) ib.m_imageWidth = gifData.m_logicalScreenWidth; else if (ib.m_imageWidth < 0) ib.m_imageWidth = 0;
        byteIndex += 2;

        // Image Height(2 Bytes)
        ib.m_imageHeight = BitConverter.ToUInt16(gifBytes, byteIndex);
        //if (ib.m_imageHeight > gifData.m_logicalScreenHeight) ib.m_imageHeight = gifData.m_logicalScreenHeight; else if (ib.m_imageHeight < 0) ib.m_imageHeight = 0;
        byteIndex += 2;

        // 1 Byte Packed field
        {
            // Local Color Table Flag(1 Bit)
            ib.m_localColorTableFlag = (gifBytes[byteIndex] & 128) == 128; // 0b10000000

            // Interlace Flag(1 Bit)
            ib.m_interlaceFlag = (gifBytes[byteIndex] & 64) == 64; // 0b01000000

            // Sort Flag(1 Bit)
            ib.m_sortFlag = (gifBytes[byteIndex] & 32) == 32; // 0b00100000

            // Reserved(2 Bits)
            // Unused

            // Size of Local Color Table(3 Bits)
            int val = (gifBytes[byteIndex] & 7) + 1;
            ib.m_sizeOfLocalColorTable = (int)Math.Pow(2, val);

            byteIndex++;
        }

        if (ib.m_localColorTableFlag)
        {
            // Local Color Table(0～255×3 Bytes)
            ib.m_localColorTable = new List<byte[]>();
            int lctEndIndex = byteIndex + (ib.m_sizeOfLocalColorTable * 3);
            for (int i = byteIndex; i < lctEndIndex; i += 3)
            {
                ib.m_localColorTable.Add(new byte[] { gifBytes[i], gifBytes[i + 1], gifBytes[i + 2] });
            }
            byteIndex = lctEndIndex;
        }

        // LZW Minimum Code Size(1 Byte)
        ib.m_lzwMinimumCodeSize = gifBytes[byteIndex];
        byteIndex++;

        // Block Size & Image Data List
        while (true)
        {
            // Block Size(1 Byte)
            byte blockSize = gifBytes[byteIndex];
            byteIndex++;

            if (blockSize == 0x00)
            {
                // Block Terminator(1 Byte)
                break;
            }

            var imageDataBlock = new ImageBlock.ImageDataBlock();
            imageDataBlock.m_blockSize = blockSize;

            // Image Data(? Bytes)
            imageDataBlock.m_imageData = new byte[imageDataBlock.m_blockSize];
            //for (int i = 0; i < imageDataBlock.m_imageData.Length; i++)
            //{
            //    imageDataBlock.m_imageData[i] = gifBytes[byteIndex];
            //    byteIndex++;
            //}
            Array.Copy(gifBytes, byteIndex, imageDataBlock.m_imageData, 0, imageDataBlock.m_blockSize);
            byteIndex += imageDataBlock.m_blockSize;

            if (ib.m_imageDataList == null)
            {
                ib.m_imageDataList = new List<ImageBlock.ImageDataBlock>();
            }
            ib.m_imageDataList.Add(imageDataBlock);
        }

        if (gifData.m_imageBlockList == null)
        {
            gifData.m_imageBlockList = new List<ImageBlock>();
        }
        gifData.m_imageBlockList.Add(ib);
    }

    private void SetGraphicControlExtension(byte[] gifBytes, ref int byteIndex, ref GifData gifData)
    {
        GraphicControlExtension gcEx = new GraphicControlExtension();

        // Extension Introducer(1 Byte)
        // 0x21
        gcEx.m_extensionIntroducer = gifBytes[byteIndex];
        byteIndex++;

        // Graphic Control Label(1 Byte)
        // 0xf9
        gcEx.m_graphicControlLabel = gifBytes[byteIndex];
        byteIndex++;

        // Block Size(1 Byte)
        // 0x04
        gcEx.m_blockSize = gifBytes[byteIndex];
        byteIndex++;

        // 1 Byte Packed field
        {
            // Reserved(3 Bits)
            // Unused

            // Disposal Mothod(3 Bits)
            // 0 (No disposal specified)
            // 1 (Do not dispose)
            // 2 (Restore to background color)
            // 3 (Restore to previous)
            switch (gifBytes[byteIndex] & 28)
            { // 0b00011100
                case 4:     // 0b00000100
                    gcEx.m_disposalMethod = 1;
                    break;
                case 8:     // 0b00001000
                    gcEx.m_disposalMethod = 2;
                    break;
                case 12:    // 0b00001100
                    gcEx.m_disposalMethod = 3;
                    break;
                default:
                    gcEx.m_disposalMethod = 0;
                    break;
            }

            // User Input Flag(1 Bit)
            // Unknown

            // Transparent Color Flag(1 Bit)
            gcEx.m_transparentColorFlag = (gifBytes[byteIndex] & 1) == 1; // 0b00000001
            byteIndex++;
        }

        // Delay Time(2 Bytes)
        gcEx.m_delayTime = BitConverter.ToUInt16(gifBytes, byteIndex);
        byteIndex += 2;

        // Transparent Color Index(1 Byte)
        gcEx.m_transparentColorIndex = gifBytes[byteIndex];
        byteIndex++;

        // Block Terminator(1 Byte)
        gcEx.m_blockTerminator = gifBytes[byteIndex];
        byteIndex++;

        if (gifData.m_graphicCtrlExList == null)
        {
            gifData.m_graphicCtrlExList = new List<GraphicControlExtension>();
        }
        gifData.m_graphicCtrlExList.Add(gcEx);
    }

    private void SetCommentExtension(byte[] gifBytes, ref int byteIndex, ref GifData gifData)
    {
        if (SkipOptionalExtensions)
        {
            byteIndex++;
            return;
        }
        int beforeByteIndex = byteIndex;
        try
        {
            CommentExtension commentEx = new CommentExtension();

            // Extension Introducer(1 Byte)
            // 0x21
            commentEx.m_extensionIntroducer = gifBytes[byteIndex];
            byteIndex++;

            // Comment Label(1 Byte)
            // 0xfe
            commentEx.m_commentLabel = gifBytes[byteIndex];
            byteIndex++;

            // Block Size & Comment Data List
            while (true)
            {
                // Block Size(1 Byte)
                byte blockSize = gifBytes[byteIndex];
                byteIndex++;

                if (blockSize == 0x00)
                {
                    // Block Terminator(1 Byte)
                    break;
                }

                // Comment Data(n Byte)
                var commentDataBlock = new CommentExtension.CommentDataBlock();
                commentDataBlock.m_blockSize = blockSize;
                commentDataBlock.m_commentData = new byte[commentDataBlock.m_blockSize];
                for (int i = 0; i < commentDataBlock.m_commentData.Length; i++)
                {
                    commentDataBlock.m_commentData[i] = gifBytes[byteIndex];
                    byteIndex++;
                }

                if (commentEx.m_commentDataList == null)
                {
                    commentEx.m_commentDataList = new List<CommentExtension.CommentDataBlock>();
                }
                commentEx.m_commentDataList.Add(commentDataBlock);
            }

            if (gifData.m_commentExList == null)
            {
                gifData.m_commentExList = new List<CommentExtension>();
            }
            gifData.m_commentExList.Add(commentEx);
        }
        catch (Exception e)
        {
            byteIndex = beforeByteIndex + 1;
#if UNITY_EDITOR
            Debug.LogWarning("Seems something wrong with the CommentExtension, just ignore it. byteIndex: " + byteIndex + "\n" + e.Message);
#endif
        }
    }

    private void SetPlainTextExtension(byte[] gifBytes, ref int byteIndex, ref GifData gifData)
    {
        if (SkipOptionalExtensions)
        {
            byteIndex++;
            return;
        }

        PlainTextExtension plainTxtEx = new PlainTextExtension();

        // Extension Introducer(1 Byte)
        // 0x21
        plainTxtEx.m_extensionIntroducer = gifBytes[byteIndex];
        byteIndex++;

        // Plain Text Label(1 Byte)
        // 0x01
        plainTxtEx.m_plainTextLabel = gifBytes[byteIndex];
        byteIndex++;

        // Block Size(1 Byte)
        // 0x0c
        plainTxtEx.m_blockSize = gifBytes[byteIndex];
        byteIndex++;

        // Text Grid Left Position(2 Bytes)
        // Not supported
        byteIndex += 2;

        // Text Grid Top Position(2 Bytes)
        // Not supported
        byteIndex += 2;

        // Text Grid Width(2 Bytes)
        // Not supported
        byteIndex += 2;

        // Text Grid Height(2 Bytes)
        // Not supported
        byteIndex += 2;

        // Character Cell Width(1 Bytes)
        // Not supported
        byteIndex++;

        // Character Cell Height(1 Bytes)
        // Not supported
        byteIndex++;

        // Text Foreground Color Index(1 Bytes)
        // Not supported
        byteIndex++;

        // Text Background Color Index(1 Bytes)
        // Not supported
        byteIndex++;

        // Block Size & Plain Text Data List
        while (true)
        {
            // Block Size(1 Byte)
            byte blockSize = gifBytes[byteIndex];
            byteIndex++;

            if (blockSize == 0x00)
            {
                // Block Terminator(1 Byte)
                break;
            }

            var plainTextDataBlock = new PlainTextExtension.PlainTextDataBlock();
            plainTextDataBlock.m_blockSize = blockSize;

            // Plain Text Data(n Byte)
            plainTextDataBlock.m_plainTextData = new byte[plainTextDataBlock.m_blockSize];
            for (int i = 0; i < plainTextDataBlock.m_plainTextData.Length; i++)
            {
                plainTextDataBlock.m_plainTextData[i] = gifBytes[byteIndex];
                byteIndex++;
            }

            if (plainTxtEx.m_plainTextDataList == null)
            {
                plainTxtEx.m_plainTextDataList = new List<PlainTextExtension.PlainTextDataBlock>();
            }
            plainTxtEx.m_plainTextDataList.Add(plainTextDataBlock);
        }

        if (gifData.m_plainTextExList == null)
        {
            gifData.m_plainTextExList = new List<PlainTextExtension>();
        }
        gifData.m_plainTextExList.Add(plainTxtEx);
    }

    private void SetApplicationExtension(byte[] gifBytes, ref int byteIndex, ref GifData gifData)
    {
        // Extension Introducer(1 Byte)
        // Now byteIndex at 0x21
        gifData.m_appEx.m_extensionIntroducer = gifBytes[byteIndex];
        byteIndex++;

        // Extension Label(1 Byte)
        // 0xff
        gifData.m_appEx.m_extensionLabel = gifBytes[byteIndex];
        byteIndex++;

        // Block Size(1 Byte)
        // 0x0b
        gifData.m_appEx.m_blockSize = gifBytes[byteIndex];
        byteIndex++;

        // Application Identifier(8 Bytes) (NETSCAPE)
        gifData.m_appEx.m_appId1 = gifBytes[byteIndex];
        byteIndex++;
        gifData.m_appEx.m_appId2 = gifBytes[byteIndex];
        byteIndex++;
        gifData.m_appEx.m_appId3 = gifBytes[byteIndex];
        byteIndex++;
        gifData.m_appEx.m_appId4 = gifBytes[byteIndex];
        byteIndex++;
        gifData.m_appEx.m_appId5 = gifBytes[byteIndex];
        byteIndex++;
        gifData.m_appEx.m_appId6 = gifBytes[byteIndex];
        byteIndex++;
        gifData.m_appEx.m_appId7 = gifBytes[byteIndex];
        byteIndex++;
        gifData.m_appEx.m_appId8 = gifBytes[byteIndex];
        byteIndex++;

        // Application Authentication Code(3 Bytes)
        gifData.m_appEx.m_appAuthCode1 = gifBytes[byteIndex];
        byteIndex++;
        gifData.m_appEx.m_appAuthCode2 = gifBytes[byteIndex];
        byteIndex++;
        gifData.m_appEx.m_appAuthCode3 = gifBytes[byteIndex];
        byteIndex++;

        // Block Size & Application Data List
        while (true)
        {
            // Block Size (1 Byte)
            byte blockSize = gifBytes[byteIndex];
            byteIndex++;

            if (blockSize == 0x00)
            {
                // Block Terminator(1 Byte)
                break;
            }

            var appDataBlock = new ApplicationExtension.ApplicationDataBlock();
            appDataBlock.m_blockSize = blockSize;

            // Application Data(n Byte)
            appDataBlock.m_applicationData = new byte[appDataBlock.m_blockSize];
            for (int i = 0; i < appDataBlock.m_applicationData.Length; i++)
            {
                appDataBlock.m_applicationData[i] = gifBytes[byteIndex];
                byteIndex++;
            }

            if (gifData.m_appEx.m_appDataList == null)
            {
                gifData.m_appEx.m_appDataList = new List<ApplicationExtension.ApplicationDataBlock>();
            }
            gifData.m_appEx.m_appDataList.Add(appDataBlock);
        }
    }

    /// <summary>
    /// Convert BitArray to int (Specifies the start index and bit length)
    /// </summary>
    /// <returns>Converted int</returns>
    /// <param name="bitData">BitArray of the compressed lzw data</param>
    /// <param name="startIndex">Start index</param>
    /// <param name="bitLength">Bit length</param>
    private int GetNumeral(BitArray bitData, int startIndex, int bitLength)
    {
        int result = 0;
        int arrayLength = bitData.Length;
        for (int i = 0; i < bitLength && arrayLength > startIndex + i; ++i)
        {
            bool bit = bitData.Get(startIndex + i);
            if (bit)
            {
                result += 1 << i;
            }
        }
        return result;
    }

    /// <summary>
    /// Clear the sprite and texture2D in the list of GifTexture
    /// </summary>
    internal void ClearGifTextures()
    {
        List<GifTexture> gifTexList = _tempGifTextures;
        if (gifTexList != null)
        {
            for (int i = 0; i < gifTexList.Count; i++)
            {
                if (gifTexList[i] != null)
                {
                    gifTexList[i].m_Colors = null;

                    if (gifTexList[i].m_texture2d != null)
                    {
                        UnityEngine.Object.Destroy(gifTexList[i].m_texture2d);
                        gifTexList[i].m_texture2d = null;
                    }

                    if (gifTexList[i].m_Sprite != null && gifTexList[i].m_Sprite.texture != null)
                    {
                        UnityEngine.Object.Destroy(gifTexList[i].m_Sprite.texture);
                        gifTexList[i].m_Sprite = null;
                    }
                }
            }
        }
    }

    #region ----- Texture Builder V1 -----
    private Color32[] SetPixel(GifData gifData, List<GifTexture> gifTexList, int decodeIndex, List<ushort> disposalMethodList, 
        int imageIndex, byte[] decodedData, List<byte[]> colorTable, Color32? bgColor, int transparentIndex)
    {
        ImageBlock imgBlock = gifData.m_imageBlockList[imageIndex];
        // Sort interlace GIF
        if (imgBlock.m_interlaceFlag)
        {
            decodedData = SortInterlace(decodedData, imgBlock.m_imageWidth);
        }

        int texWidth = gifData.m_logicalScreenWidth;
        int texHeight = gifData.m_logicalScreenHeight;

        bool filledPixels = false;
        bool useBeforeTex = false;

        int beforeIndex = -1;

        // Check dispose
        ushort lastDisposalMethod = decodeIndex > 0 ? disposalMethodList[decodeIndex - 1] : (ushort)2;
        if (decodeIndex > 0 && lastDisposalMethod == 0 || lastDisposalMethod == 1)
        {
            // before 1
            beforeIndex = decodeIndex - 1;
        }
        else if (lastDisposalMethod == 2)
        {
            filledPixels = true;
        }
        else if (decodeIndex > 1 && lastDisposalMethod == 3)
        {
            // Restore to the previous block
            for (int i = decodeIndex - 1; i >= 0; i--)
            {
                if (disposalMethodList[i] == 0 || disposalMethodList[i] == 1)
                {
                    beforeIndex = i;
                    break;
                }
            }
        }

        if (_debugLog)
        {
            ImageBlock ib = gifData.m_imageBlockList[imageIndex];
            Debug.Log(" lastDisposalMethod: " + lastDisposalMethod + " W: " + ib.m_imageWidth + " H: " + ib.m_imageHeight
                + " T: " + ib.m_imageTopPosition + " L: " + ib.m_imageLeftPosition
                + " Logic W: " + gifData.m_logicalScreenWidth + " Logic H: " + gifData.m_logicalScreenHeight
                + " | DecoddeData: " + decodedData.Length);
        }

        Color32[] pix;
        if (beforeIndex >= 0)
        {
            // Do not dispose
            pix = (Color32[])gifTexList[beforeIndex].m_Colors.Clone();
            useBeforeTex = true;
            filledPixels = true;
        }
        else
        {
            pix = new Color32[texWidth * texHeight];
        }

        // Set pixel data: Reverse set pixels. because GIF data starts from the top left.
        int dataIndex = 0;
        for (int y = texHeight - 1; y >= 0; y--)
        {
            SetPixelRow(ref pix, texWidth, texHeight, y, gifData.m_imageBlockList[imageIndex], decodedData,
                ref dataIndex, colorTable, bgColor, transparentIndex, useBeforeTex, filledPixels);
        }

        return pix;
    }

    /// <summary>
    /// Set texture pixel row
    /// </summary>
    private void SetPixelRow(ref Color32[] pixels, int texWidth, int texHeight, int y, ImageBlock imgBlock, byte[] decodedData,
        ref int dataIndex, List<byte[]> colorTable, Color32? bgColor, int transparentIndex, bool useBeforeTex, bool filledPixels)
    {
        // Row no (0~)
        int row = texHeight - 1 - y;
        int startX = imgBlock.m_imageLeftPosition;
        int startY = imgBlock.m_imageTopPosition;
        int endX = imgBlock.m_imageLeftPosition + imgBlock.m_imageWidth;
        int endY = imgBlock.m_imageTopPosition + imgBlock.m_imageHeight;

        Color32 c0 = new Color32(0, 0, 0, 0);

        for (int x = 0; x < texWidth; x++)
        {
            // Line no (0~)
            int line = x;

            // Out of image blocks
            if (row < startY || row >= endY || line < startX || line >= endX)
            {
                if (!useBeforeTex && !filledPixels)
                {
                    pixels[x + y * texWidth] = transparentIndex < 0 ? bgColor.Value : c0;
                }
                continue;
            }

            // Out of decoded data
            if (dataIndex >= decodedData.Length)
            {
                if (!filledPixels)
                {
                    pixels[x + y * texWidth] = Color.black;
                }
                continue;
            }

            // Get pixel color from color table
            byte colorIndex = decodedData[dataIndex];
            if (colorTable == null || colorTable.Count <= colorIndex)
            {
                if (!filledPixels)
                {
                    pixels[x + y * texWidth] = bgColor.Value;
                }
                continue;
            }

            // Set pixels
            byte[] rgb = colorTable[colorIndex];
            byte alpha = (byte)(transparentIndex >= 0 && transparentIndex == colorIndex ? 0 : 255);
            if (!filledPixels || alpha != 0 || !useBeforeTex)
            {
                pixels[x + y * texWidth] = new Color32(rgb[0], rgb[1], rgb[2], alpha);
            }

            dataIndex++;
        }
    }

    /// <summary>
    /// Sort interlace GIF data
    /// </summary>
    /// <param name="decodedData">Decoded GIF data</param>
    /// <param name="xNum">Pixel number of horizontal row</param>
    /// <returns>Sorted data</returns>
    private byte[] SortInterlace(byte[] decodedData, int xNum)
    {
        int rowNo = 0;
        int dataIndex = 0;
        byte[] newArr = new byte[decodedData.Length];
        // Every 8th. row, starting with row 0.
        for (int i = 0; i < newArr.Length; i++)
        {
            if (rowNo % 8 == 0)
            {
                newArr[i] = decodedData[dataIndex];
                dataIndex++;
            }
            if (i != 0 && i % xNum == 0)
            {
                rowNo++;
            }
        }
        rowNo = 0;
        // Every 8th. row, starting with row 4.
        for (int i = 0; i < newArr.Length; i++)
        {
            if (rowNo % 8 == 4)
            {
                newArr[i] = decodedData[dataIndex];
                dataIndex++;
            }
            if (i != 0 && i % xNum == 0)
            {
                rowNo++;
            }
        }
        rowNo = 0;
        // Every 4th. row, starting with row 2.
        for (int i = 0; i < newArr.Length; i++)
        {
            if (rowNo % 4 == 2)
            {
                newArr[i] = decodedData[dataIndex];
                dataIndex++;
            }
            if (i != 0 && i % xNum == 0)
            {
                rowNo++;
            }
        }
        rowNo = 0;
        // Every 2nd. row, starting with row 1.
        for (int i = 0; i < newArr.Length; i++)
        {
            if (rowNo % 8 != 0 && rowNo % 8 != 4 && rowNo % 4 != 2)
            {
                newArr[i] = decodedData[dataIndex];
                dataIndex++;
            }
            if (i != 0 && i % xNum == 0)
            {
                rowNo++;
            }
        }
        return newArr;
    }
    #endregion


    #region ----- LzwDecode (v1) -----
    /// <summary>
    /// GIF LZW decode method (version1)
    /// </summary>
    /// <param name="compData">LZW compressed data</param>
    /// <param name="lzwMinimumCodeSize">LZW minimum code size</param>
    /// <param name="needDataSize">Need decoded data size</param>
    /// <returns>Decoded data array</returns>
    private byte[] LzwDecodeV1(byte[] compData, int lzwMinimumCodeSize, int needDataSize)
    {
        if (needDataSize <= 0) return _lastBlockData;

        int clearCode = 0;
        int finishCode = 0;

        // Initialize dictionary
        Dictionary<int, string> dic = new Dictionary<int, string>();

        int lzwCodeSize = 0;
        InitDictionary(dic, lzwMinimumCodeSize, out lzwCodeSize, out clearCode, out finishCode);
        int dicCount = dic.Count;

        // Convert to bit array
        BitArray bitData = new BitArray(compData);

        byte[] output = new byte[needDataSize];
        int outputAddIndex = 0;

        string prevEntry = null;

        bool dicInitFlag = false;

        int bitDataIndex = 0;

        // LZW decode loop
        while (bitDataIndex < bitData.Length)
        {
            if (dicInitFlag)
            {
                InitDictionary(dic, lzwMinimumCodeSize, out lzwCodeSize, out clearCode, out finishCode);
                dicCount = dic.Count;
                dicInitFlag = false;
            }

            int key = GetNumeral(bitData, bitDataIndex, lzwCodeSize);

            string entry = null;

            if (key == clearCode)
            {
                // Clear (Initialize dictionary)
                dicInitFlag = true;
                bitDataIndex += lzwCodeSize;
                prevEntry = null;
                continue;
            }
            else if (key == finishCode)
            {
                if (outputAddIndex < needDataSize) //early stopped
                {
                    // Malformed GIF?
                    //Debug.LogWarning("Early stop code. Seems the compressed data corrupted before this bit: " + bitDataIndex + "."
                    //  + "\nCurrent output data index is " + outputAddIndex + " and the required output data size is " + needDataSize);
                }

                // Exit
                //Debug.LogWarning("Early stop code. bitDataIndex:" + bitDataIndex + " lzwCodeSize:" + lzwCodeSize + " key:" + key + " dic.Count:" + dic.Count);
                break;
            }
            else if (dic.ContainsKey(key))
            {
                // Output from dictionary
                entry = dic[key];
            }
            else if (key >= dicCount)
            {
                if (prevEntry != null)
                {
                    // Output from estimation
                    entry = prevEntry + prevEntry[0];
                }
                else
                {
                    Debug.LogWarningFormat("1. It is strange that come here. bitDataIndex:{0} lzwCodeSize:{1} key:{2} dic.Count:{3}", bitDataIndex, lzwCodeSize, key, dic.Count);
                    bitDataIndex += lzwCodeSize;
                    continue;
                }
            }
            else
            {
                Debug.LogWarningFormat("2. It is strange that come here. bitDataIndex:{0} lzwCodeSize:{1} key:{2} dic.Count:{3}", bitDataIndex, lzwCodeSize, key, dic.Count);
                bitDataIndex += lzwCodeSize;
                continue;
            }

            // Output
            // Take out 8 bits from the string.
            byte[] temp = Encoding.Unicode.GetBytes(entry);
            for (int i = 0; i < temp.Length; i++)
            {
                if (i % 2 == 0)
                {
                    output[outputAddIndex] = temp[i];
                    outputAddIndex++;
                }
            }

            if (outputAddIndex >= needDataSize)
            {
                // Exit
                break;
            }

            if (prevEntry != null)
            {
                // Add to dictionary
                dic.Add(dicCount, prevEntry + entry[0]);
                ++dicCount;
            }

            prevEntry = entry;

            bitDataIndex += lzwCodeSize;

            if (lzwCodeSize == 3 && dicCount >= 8)
            {
                lzwCodeSize = 4;
            }
            else if (lzwCodeSize == 4 && dicCount >= 16)
            {
                lzwCodeSize = 5;
            }
            else if (lzwCodeSize == 5 && dicCount >= 32)
            {
                lzwCodeSize = 6;
            }
            else if (lzwCodeSize == 6 && dicCount >= 64)
            {
                lzwCodeSize = 7;
            }
            else if (lzwCodeSize == 7 && dicCount >= 128)
            {
                lzwCodeSize = 8;
            }
            else if (lzwCodeSize == 8 && dicCount >= 256)
            {
                lzwCodeSize = 9;
            }
            else if (lzwCodeSize == 9 && dicCount >= 512)
            {
                lzwCodeSize = 10;
            }
            else if (lzwCodeSize == 10 && dicCount >= 1024)
            {
                lzwCodeSize = 11;
            }
            else if (lzwCodeSize == 11 && dicCount >= 2048)
            {
                lzwCodeSize = 12;
            }
            else if (lzwCodeSize == 12 && dicCount >= 4096)
            {
                int nextKey = GetNumeral(bitData, bitDataIndex, lzwCodeSize);
                if (nextKey != clearCode)
                {
                    dicInitFlag = true;
                }
            }
        }

        _lastBlockData = output;
        return output;
    }

    /// <summary>
    /// Initialize dictionary
    /// </summary>
    /// <param name="dic">Dictionary</param>
    /// <param name="lzwMinimumCodeSize">LZW minimum code size</param>
    /// <param name="lzwCodeSize">out LZW code size</param>
    /// <param name="clearCode">out Clear code</param>
    /// <param name="finishCode">out Finish code</param>
    private void InitDictionary(Dictionary<int, string> dic, int lzwMinimumCodeSize, out int lzwCodeSize, out int clearCode, out int finishCode)
    {
        int dicLength = (int)Math.Pow(2, lzwMinimumCodeSize);

        clearCode = dicLength;
        finishCode = clearCode + 1;

        dic.Clear();

        for (int i = 0; i < dicLength + 2; i++)
        {
            dic.Add(i, ((char)i).ToString());
        }

        lzwCodeSize = lzwMinimumCodeSize + 1;
    }
    #endregion


    #region ----- LzwDecode (v2) -----
    /// <summary>
    /// GIF LZW decode method (version 2)
    /// </summary>
    /// <param name="lzwData">LZW compressed data</param>
    /// <param name="lzwMinCodeSize">LZW minimum code size</param>
    /// <param name="needDataSize">Need decoded data size</param>
    /// <returns>Decoded data array</returns>
    private byte[] LzwDecodeV2(byte[] lzwData, int lzwMinCodeSize, int needDataSize)
    {
        if (needDataSize <= 0) return _lastBlockData;
        // Create an array for pixels
        byte[] output = new byte[needDataSize];

        int MaxStack = 0x1000; //4096
        int NullCode = -1;

        int codeSize = lzwMinCodeSize + 1;
        int clearFlag = 1 << lzwMinCodeSize;
        int endFlag = clearFlag + 1;

        int currStack = endFlag + 1;
        int[] prefix = new int[MaxStack];
        int[] suffix = new int[MaxStack];
        int[] pixelStack = new int[MaxStack + 1];

        int code = NullCode;
        int oldCode = NullCode;
        int codeMask = (1 << codeSize) - 1;
        int bits = 0;

        int px = 0;
        int pxCount = 0;
        int currPx = 0;
        int bitCodeIdx = 0;

        int data = 0;
        int first = 0;
        int inCode = NullCode;

        for (code = 0; code < clearFlag; code++)
        {
            prefix[code] = 0;
            suffix[code] = (byte)code;
        }
        
        while (currPx < needDataSize)
        {
            if (px == 0)
            {
                if (bits < codeSize)
                {
                    if (pxCount == 0)
                    {
                        // Get the lzw data size
                        pxCount = lzwData.Length;
                        if (pxCount == 0) break;
                        bitCodeIdx = 0;
                    }

                    data += lzwData[bitCodeIdx] << bits;
                    bits += 8;
                    bitCodeIdx++;
                    pxCount--;
                    continue;
                }

                code = data & codeMask;
                data >>= codeSize;
                bits -= codeSize;

                if (code > currStack || code == endFlag) break;

                if (code == clearFlag)
                {
                    codeSize = lzwMinCodeSize + 1;
                    codeMask = (1 << codeSize) - 1;
                    currStack = clearFlag + 2;
                    oldCode = NullCode;
                    continue;
                }

                if (oldCode == NullCode)
                {
                    pixelStack[px++] = suffix[code];
                    oldCode = code;
                    first = code;
                    continue;
                }

                inCode = code;

                if (code == currStack)
                {
                    pixelStack[px++] = (byte)first;
                    code = oldCode;
                }

                while (code > clearFlag)
                {
                    pixelStack[px++] = suffix[code];
                    code = prefix[code];
                }

                first = suffix[code];
                pixelStack[px++] = (byte)first;

                if (currStack < MaxStack)
                {
                    prefix[currStack] = (ushort)oldCode;
                    suffix[currStack] = (byte)first;
                }

                currStack++;

                if (((currStack & codeMask) == 0) && (currStack < MaxStack))
                {
                    codeSize++;
                    codeMask |= currStack;
                }

                oldCode = inCode;
            }

            px--;
            output[currPx++] = (byte)pixelStack[px];
        }

        _lastBlockData = output;
        return output;
    }
    #endregion

}
