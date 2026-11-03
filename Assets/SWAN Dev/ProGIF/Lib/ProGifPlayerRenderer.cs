// Created by SwanDEV 2017

using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public sealed class ProGifPlayerRenderer : ProGifPlayerComponent
{
	[HideInInspector] public Renderer destinationRenderer;				// The renderer for displaying textures
	private List<Renderer> m_ExtraRenderers = new List<Renderer>();

	private Texture2D _displayTexture2D = null;

	void Awake()
	{
		if(destinationRenderer == null)
		{
			destinationRenderer = gameObject.GetComponent<Renderer>();
            displayType = DisplayType.Renderer;
        }
	}

	// Update gif frame for the Player (Update is called once per frame)
	void Update()
	{
		base.ThreadsUpdate();

		if(State == PlayerState.Playing && displayType == DisplayType.Renderer)
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

					if(m_ExtraRenderers != null && m_ExtraRenderers.Count > 0)
					{
						Texture2D tex = null;
						if(optimizeMemoryUsage)
						{
							tex = _displayTexture2D;
						}
						else
						{
							tex = gifTextures[spriteIndex].GetTexture2D();
						}

						for(int i = 0; i < m_ExtraRenderers.Count; i++)
						{
							if(m_ExtraRenderers[i] != null)
							{
								m_ExtraRenderers[i].material.mainTexture = tex;
							}
							else
							{
								m_ExtraRenderers.RemoveAt(i);
								m_ExtraRenderers.TrimExcess();
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

		destinationRenderer = gameObject.GetComponent<Renderer>();
        displayType = DisplayType.Renderer;
        _SetDisplay(0);
	}

    protected override void _OnFrameReady(GifTexture gTex, bool isFirstFrame)
	{
        if (isFirstFrame)
        {
            displayType = DisplayType.Renderer;
            _SetDisplay(0);
        }
	}

	private void _SetDisplay(int frameIndex)
	{
		if(optimizeMemoryUsage)
		{
			gifTextures[frameIndex].SetColorsToTexture2D(ref _displayTexture2D);
		}

		if(destinationRenderer != null && destinationRenderer.material != null) 
		{
			if(optimizeMemoryUsage)
			{
				destinationRenderer.material.mainTexture = _displayTexture2D;
			}
			else
			{
				destinationRenderer.material.mainTexture = gifTextures[frameIndex].GetTexture2D();
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

	public void ChangeDestination(Renderer renderer)
	{
        if(destinationRenderer != null && destinationRenderer.material != null) destinationRenderer.material.mainTexture = null;
        destinationRenderer = renderer;
	}

	public void AddExtraDestination(Renderer renderer)
	{
		if(!m_ExtraRenderers.Contains(renderer))
		{
			m_ExtraRenderers.Add(renderer);
		}
	}

	public void RemoveFromExtraDestination(Renderer renderer)
	{
		if(m_ExtraRenderers.Contains(renderer))
		{
			m_ExtraRenderers.Remove(renderer);
			m_ExtraRenderers.TrimExcess();
		}
	}
}
