// Created by SwanDEV 2018

using System;

/// <summary>
/// For getting the target GIF info. (The first frame texture, width, height, fps, total frame count, loop count, etc.)
/// </summary>
public class ProGifInfo : ProGifPlayerComponent
{
	public void GetInfo(string loadPath, Action<FirstGifFrame> onComplete, Decoder decoder = Decoder.ProGif_QueuedThread)
	{
		onComplete +=(firstFrame)=>{
			Clear();
			Destroy(gameObject);
		};
		SetAdvancedDecodeSettings(decoder, 1);
		SetOnFirstFrameCallback(onComplete);
		LoadGifFromUrl(loadPath);
		this.loadPath = loadPath;
	}

    public void GetInfo(string loadPath, Action<ProGifDecoder.GifInfo> onComplete)
    {
        StartCoroutine(LoadGifRoutine(loadPath, (gifBytes) =>
        {
            onComplete(new ProGifDecoder().GetGifInfo(gifBytes));
            Clear();
            Destroy(gameObject);
        }));
    }

    void Update()
	{
		base.ThreadsUpdate();
	}

    protected override void _OnFrameReady(GifTexture gTex, bool isFirstFrame){}
}
