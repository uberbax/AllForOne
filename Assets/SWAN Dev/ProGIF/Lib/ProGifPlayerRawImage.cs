// Created by SwanDEV 2017

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public sealed class ProGifPlayerRawImage : ProGifPlayerComponent
{
    [HideInInspector] public RawImage destinationRawImage;                        // The RawImage for displaying textures
    private List<RawImage> m_ExtraRawImages = new List<RawImage>();

	private Texture2D _displayTexture2D = null;

    void Awake()
    {
        if (destinationRawImage == null)
        {
            destinationRawImage = gameObject.GetComponent<RawImage>();
            displayType = DisplayType.RawImage;
        }
    }

    // Update gif frame for the Player (Update is called once per frame)
    void Update()
    {
        base.ThreadsUpdate();

        if (State == PlayerState.Playing && displayType == DisplayType.RawImage)
        {
            float time = ignoreTimeScale ? Time.unscaledTime : Time.time;
            float dt = Mathf.Min(time - nextFrameTime, interval);
            if (dt >= 0f)
            {
                spriteIndex = (spriteIndex >= gifTextures.Count - 1) ? 0 : spriteIndex + 1;
                nextFrameTime = time + interval / playbackSpeed - dt;

                if (spriteIndex < gifTextures.Count)
	            {
	                if (OnPlayingCallback != null) OnPlayingCallback(gifTextures[spriteIndex]);

					_SetDisplay(spriteIndex);

	                if(m_ExtraRawImages != null && m_ExtraRawImages.Count > 0)
	                {
						Texture2D  tex = null;
						if(optimizeMemoryUsage)
						{
							tex = _displayTexture2D;
						}
						else
						{
							tex = gifTextures[spriteIndex].GetTexture2D();
						}

	                    for(int i = 0; i < m_ExtraRawImages.Count; i++)
	                    {
	                        if(m_ExtraRawImages[i] != null)
	                        {
	                            m_ExtraRawImages[i].texture = tex;
	                        }
	                        else
	                        {
	                            m_ExtraRawImages.RemoveAt(i);
	                            m_ExtraRawImages.TrimExcess();
	                        }
	                    }
	                }
	            }
			}
        }
    }

	public override void Play(RenderTexture[] gifFrames, float fps, bool isCustomRatio, int customWidth, int customHeight, bool optimizeMemoryUsage)
	{
		base.Play(gifFrames, fps, isCustomRatio, customWidth, customHeight, optimizeMemoryUsage);

		destinationRawImage = gameObject.GetComponent<RawImage>();
        displayType = DisplayType.RawImage;
        _SetDisplay(0);
	}

    protected override void _OnFrameReady(GifTexture gTex, bool isFirstFrame)
    {
        if (isFirstFrame)
        {
            displayType = DisplayType.RawImage;
            _SetDisplay(0);
        }
    }

	private void _SetDisplay(int frameIndex)
	{
		if(optimizeMemoryUsage)
		{
			gifTextures[frameIndex].SetColorsToTexture2D(ref _displayTexture2D);
		}

		if(destinationRawImage != null)
		{
			if(optimizeMemoryUsage)
			{
				destinationRawImage.texture = _displayTexture2D;
			}
			else
			{
				destinationRawImage.texture = gifTextures[frameIndex].GetTexture2D();
			}
		}
	}

    public override void Clear(bool clearBytes = true, bool clearCallbacks = true)
    {
		if(_displayTexture2D != null) 
		{
			Destroy(_displayTexture2D);
		}
        base.Clear(clearBytes, clearCallbacks);
    }

    public void ChangeDestination(RawImage rawImage)
    {
        if (destinationRawImage != null) destinationRawImage.texture = null;
        destinationRawImage = rawImage;
    }

    public void AddExtraDestination(RawImage rawImage)
    {
        if(!m_ExtraRawImages.Contains(rawImage))
        {
            m_ExtraRawImages.Add(rawImage);
        }
    }

    public void RemoveFromExtraDestination(RawImage rawImage)
    {
        if(m_ExtraRawImages.Contains(rawImage))
        {
            m_ExtraRawImages.Remove(rawImage);
            m_ExtraRawImages.TrimExcess();
        }
    }
}
