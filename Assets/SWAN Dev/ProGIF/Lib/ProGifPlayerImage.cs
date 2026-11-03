// Created by SwanDEV 2017

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public sealed class ProGifPlayerImage : ProGifPlayerComponent
{
	[HideInInspector] public Image destinationImage;						// The image for displaying sprites
	private List<Image> m_ExtraImages = new List<Image>();

	private Texture2D _displayTexture2D = null;
	private Sprite _displaySprite = null;

	void Awake()
	{
		if(destinationImage == null)
		{
			destinationImage = gameObject.GetComponent<Image>();
            displayType = DisplayType.Image;
		}
	}

	// Update gif frame for the Player (Update is called once per frame)
	void Update()
	{
		base.ThreadsUpdate();
        
		if(State == PlayerState.Playing && displayType == DisplayType.Image)
        {
            float time = ignoreTimeScale ? Time.unscaledTime : Time.time;
            float dt = Mathf.Min(time - nextFrameTime, interval);
            if (dt >= 0f)
            {
				spriteIndex = (spriteIndex >= gifTextures.Count - 1)? 0 : spriteIndex + 1;
                nextFrameTime = time + interval / playbackSpeed - dt;

                if (spriteIndex < gifTextures.Count)
				{
					if(OnPlayingCallback != null) OnPlayingCallback(gifTextures[spriteIndex]);

					_SetDisplay(spriteIndex);

					if(m_ExtraImages != null && m_ExtraImages.Count > 0)
					{
						Sprite sp = null;
						if(optimizeMemoryUsage)
						{
							sp = _displaySprite;
						}
						else
						{
							sp = gifTextures[spriteIndex].GetSprite();
						}

						for(int i = 0; i < m_ExtraImages.Count; i++)
						{
							if(m_ExtraImages[i] != null)
							{
								m_ExtraImages[i].sprite = sp;
							}
							else
							{
								m_ExtraImages.RemoveAt(i);
								m_ExtraImages.TrimExcess();
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

		destinationImage = gameObject.GetComponent<UnityEngine.UI.Image>();
        displayType = DisplayType.Image;
        _SetDisplay(0);
	}

    protected override void _OnFrameReady(GifTexture gTex, bool isFirstFrame)
    {
        if (isFirstFrame)
        {
            displayType = DisplayType.Image;
            _SetDisplay(0);
        }
	}

	private void _SetDisplay(int frameIndex)
	{
		if(optimizeMemoryUsage)
		{
			_displaySprite = gifTextures[frameIndex].GetSprite_OptimizeMemoryUsage(ref _displayTexture2D);
		}

		if(destinationImage != null)
		{
			if(optimizeMemoryUsage)
			{
				destinationImage.sprite = _displaySprite;
			}
			else
			{
				destinationImage.sprite = gifTextures[frameIndex].GetSprite();
			}
		}
	}

	public override void Clear(bool clearBytes = true, bool clearCallbacks = true)
	{
		if(optimizeMemoryUsage)
		{
			if(_displayTexture2D != null) 
			{
				Destroy(_displayTexture2D);
				_displayTexture2D = null;
			}

			_displaySprite = null;
		}
		base.Clear(clearBytes, clearCallbacks);
	}

	public void ChangeDestination(UnityEngine.UI.Image image)
    {
        if (destinationImage != null) destinationImage.sprite = null;
        destinationImage = image;
	}

	public void AddExtraDestination(UnityEngine.UI.Image image)
	{
		if(!m_ExtraImages.Contains(image))
		{
			m_ExtraImages.Add(image);
		}
	}

	public void RemoveFromExtraDestination(UnityEngine.UI.Image image)
	{
		if(m_ExtraImages.Contains(image))
		{
			m_ExtraImages.Remove(image);
			m_ExtraImages.TrimExcess();
		}
	}
}
