// Created by SwanDEV 2017

#if PRO_GIF_GUITEXTURE
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(GUITexture))]
public sealed class ProGifPlayerGuiTexture : ProGifPlayerComponent
{
    [HideInInspector] public GUITexture destinationGuiTexture;              // The GUITexture for displaying textures
    private List<GUITexture> m_ExtraGuiTextures = new List<GUITexture>();

    private Texture2D _displayTexture2D = null;

    void Awake()
    {
        if(destinationGuiTexture == null)
        {
            destinationGuiTexture = gameObject.GetComponent<GUITexture>();
            displayType = DisplayType.GuiTexture;
        }
    }

    // Update gif frame for the Player (Update is called once per frame)
    void Update()
    {
        base.ThreadsUpdate();

        if(State == PlayerState.Playing && displayType == DisplayType.GuiTexture)
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

                    if(m_ExtraGuiTextures != null && m_ExtraGuiTextures.Count > 0)
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

                        for(int i = 0; i < m_ExtraGuiTextures.Count; i++)
                        {
                            if(m_ExtraGuiTextures[i] != null)
                            {
                                m_ExtraGuiTextures[i].texture = tex;
                            }
                            else
                            {
                                m_ExtraGuiTextures.RemoveAt(i);
                                m_ExtraGuiTextures.TrimExcess();
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

        destinationGuiTexture = gameObject.GetComponent<GUITexture>();
        displayType = DisplayType.GuiTexture;
        _SetDisplay(0);
    }

    protected override void _OnFrameReady(GifTexture gTex, bool isFirstFrame)
    {
        if(isFirstFrame)
        {
            displayType = DisplayType.GuiTexture;
            _SetDisplay(0);
        }
    }

    private void _SetDisplay(int frameIndex)
    {
        if(optimizeMemoryUsage) 
        {
            gifTextures[frameIndex].SetColorsToTexture2D(ref _displayTexture2D);
        }

        if(destinationGuiTexture != null)
        {
            if(optimizeMemoryUsage)
            {
                destinationGuiTexture.texture = _displayTexture2D;
            }
            else
            {
                destinationGuiTexture.texture = gifTextures[frameIndex].GetTexture2D();
            }
        }
    }

    public override void Clear(bool clearBytes = true, bool clearCallbacks = true)
    {
        if(optimizeMemoryUsage && _displayTexture2D != null) 
        {
            Destroy(_displayTexture2D);
        }
        base.Clear(clearBytes, clearCallbacks);
    }

    public void ChangeDestination(GUITexture guiTexture)
    {
        if (destinationGuiTexture != null) destinationGuiTexture.texture = null;
        destinationGuiTexture = guiTexture;
    }

    public void AddExtraDestination(GUITexture guiTexture)
    {
        if(!m_ExtraGuiTextures.Contains(guiTexture))
        {
            m_ExtraGuiTextures.Add(guiTexture);
        }
    }

    public void RemoveFromExtraDestination(GUITexture guiTexture)
    {
        if(m_ExtraGuiTextures.Contains(guiTexture))
        {
            m_ExtraGuiTextures.Remove(guiTexture);
            m_ExtraGuiTextures.TrimExcess();
        }
    }
}
#endif
