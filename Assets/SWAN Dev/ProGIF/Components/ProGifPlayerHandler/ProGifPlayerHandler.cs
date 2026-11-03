/// <summary>
/// Created by SWAN DEV
/// </summary>
using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Auto load and play a single GIF by path or URL.
/// Usage : Attach this script on a GameObject/Prefab, together with ProGifPlayerImage or ProGifPlayerRawImage component.
/// Set your GIF path or URL. The GIF will be loaded and played automatically on start.
/// </summary>
public class ProGifPlayerHandler : MonoBehaviour
{
    public ProGifPlayerComponent m_PlayerComponent;
    
    public FilePathName.AppPath m_LocalDirectory = FilePathName.AppPath.StreamingAssetsPath;

    public string m_FolderName;

    public string m_FileName;

    public bool m_AutoPlayOnEnable = true;

    [Tooltip("Set size using ImageDisplayHandler settings, or set the Native Size when the GIF started to play.")]
    public bool m_SetSizeOnLoaded = true;

    public DImageDisplayHandler m_ImageDisplayHandler;
    
    [Space]
    public bool m_PlayWebGif;

    public string m_WebGifUrl;

    private void OnEnable()
    {
        if (m_PlayerComponent == null) m_PlayerComponent = GetComponent<ProGifPlayerComponent>();

        if (m_AutoPlayOnEnable)
        {
            Play();
        }
    }

    private void OnDestroy()
    {
        m_PlayerComponent.Clear();
    }

    public void Play()
    {
        if ((!m_PlayWebGif && string.IsNullOrEmpty(m_FileName)) || (m_PlayWebGif && string.IsNullOrEmpty(m_WebGifUrl)))
        {
            Debug.Log("Invalid GIF path! Missing filename or URL?");
            return;
        }

        string gifPath = string.Empty;
        if (m_PlayWebGif)
        {
            gifPath = m_WebGifUrl;
        }
        else
        {
            string directory = FilePathName.Instance.GetAppPath(m_LocalDirectory);
            gifPath = directory;
            if (!string.IsNullOrEmpty(m_FolderName))
            {
                gifPath = Path.Combine(gifPath, m_FolderName);
            }
            gifPath = Path.Combine(gifPath, m_FileName);

            if (!m_FileName.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
            {
                gifPath += ".gif";
            }
        }
        
        m_PlayerComponent.Play(gifPath, false);

        if (m_SetSizeOnLoaded)
        {
            if (m_ImageDisplayHandler == null)
            {
                m_ImageDisplayHandler = m_PlayerComponent.GetComponent<DImageDisplayHandler>();
            }

            m_PlayerComponent.SetOnFirstFrameCallback((firstFrame) =>
            {
                Image image = GetComponent<Image>();
                if (image)
                {
                    if (m_ImageDisplayHandler)
                    {
                        m_ImageDisplayHandler.SetImage(image, image.sprite);
                    }
                    else
                    {
                        image.SetNativeSize();
                    }
                }
                RawImage rawImage = GetComponent<RawImage>();
                if (rawImage)
                {
                    if (m_ImageDisplayHandler)
                    {
                        m_ImageDisplayHandler.SetRawImage(rawImage, rawImage.texture);
                    }
                    else
                    {
                        rawImage.SetNativeSize();
                    }
                }
            });
        }
    }
}
