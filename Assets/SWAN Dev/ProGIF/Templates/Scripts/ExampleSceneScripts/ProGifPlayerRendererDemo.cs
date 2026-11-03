using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This demo shows how to display gif on renderer(s), and shows how to set, add and change the display targets. 
/// (Note that the methods used in this example can also apply on Image, RawImage, and GuiTexture)
/// </summary>
public class ProGifPlayerRendererDemo : MonoBehaviour
{
	public bool m_IsMultiGifPlayer = false;

	public string m_GifPath;

    public Renderer[] m_TargetRenderers;

    public Slider m_ProgressSlider;
	public Text m_ProgressText;

	private Renderer _lastPGifRenderer;


    public void PlayOnRenderer(int index)
    {
        RemoveFromExtraDestination();
        this.index = index;
        _Play(m_TargetRenderers[index]);
    }

	private void _Play(Renderer targetRenderer)
	{
		if(targetRenderer != null)
		{
            if (m_IsMultiGifPlayer)
			{
#if UNITY_WEBGL
                PGif.iSetAdvancedPlayerDecodeSettings(ProGifPlayerComponent.Decoder.ProGif_Coroutines);
#endif
                PGif.iPlayGif(m_GifPath, targetRenderer, targetRenderer.name, (progress)=>{
					//Set the gif size when the first frame decode is finished and assigned to targetRenderer
					//Set renderer transform scale here:
					int gifWidth = PGif.iGetPlayer(targetRenderer.name).width;
					int gifHeight = PGif.iGetPlayer(targetRenderer.name).height;
					//targetRenderer.gameObject.GetComponent<Transform>().localScale = new Vector3(gifWidth/2, gifHeight/2, 
					//	targetRenderer.gameObject.GetComponent<Transform>().localScale.z);

					_OnLoading(progress);

				}, shouldSaveFromWeb:false);

                // Reset the target renderer (required if the target destination display object is changed before, else the gif frames will display on the last set destination) 
                PGif.iGetPlayer(targetRenderer.name).ChangeDestination(targetRenderer);

				_lastPGifRenderer = targetRenderer;
			}
			else
			{
#if UNITY_WEBGL
                ProGifManager.Instance.SetAdvancedPlayerDecodeSettings(ProGifPlayerComponent.Decoder.ProGif_Coroutines);
#endif
                ProGifManager.Instance.PlayGif(m_GifPath, targetRenderer, (progress)=>{
					//Set the gif size when the first frame decode is finished and assigned to targetRenderer
					//Set renderer transform scale here:
					int gifWidth = ProGifManager.Instance.m_GifPlayer.width;
					int gifHeight = ProGifManager.Instance.m_GifPlayer.height;
                    //targetRenderer.gameObject.GetComponent<Transform>().localScale = new Vector3(gifWidth/2, gifHeight/2, 
                    //	targetRenderer.gameObject.GetComponent<Transform>().localScale.z);
                    
					_OnLoading(progress);

				}, shouldSaveFromWeb:false);

                // Reset the target renderer (required if the target destination display object is changed before, else the gif frames will display on the last set destination) 
                ProGifManager.Instance.m_GifPlayer.ChangeDestination(targetRenderer);
			}
		}
	}

    private void _OnLoading(float progress)
	{
		m_ProgressSlider.value = progress;
		m_ProgressText.text = "Progress : " + Mathf.CeilToInt(progress * 100) + "%";
	}

	int index = 0;
	public void ChangeDestination()
	{
        RemoveFromExtraDestination();

		index++;
		if(index >= m_TargetRenderers.Length) index = 0;

		if(m_IsMultiGifPlayer)
		{
			if(_lastPGifRenderer == null || PGif.iGetPlayer(_lastPGifRenderer.name) == null)
			{
				index--;
				return;
			}
			PGif.iGetPlayer(_lastPGifRenderer.name).ChangeDestination(m_TargetRenderers[index]);
		}
		else
		{
			if(ProGifManager.Instance.m_GifPlayer == null)
			{
				index--;
				return;
			}
			ProGifManager.Instance.m_GifPlayer.ChangeDestination(m_TargetRenderers[index]);
		}
    }

    public void AddExtraDestination()
	{
		if(m_IsMultiGifPlayer)
		{
			if(_lastPGifRenderer == null || PGif.iGetPlayer(_lastPGifRenderer.name) == null)
			{
				return;
			}

			foreach(Renderer renderer in m_TargetRenderers)
			{
				PGif.iGetPlayer(_lastPGifRenderer.name).AddExtraDestination(renderer);
			}
		}
		else
		{
			if(ProGifManager.Instance.m_GifPlayer == null)
			{
				return;
			}

            // Add all renderer as display target
			foreach(Renderer renderer in m_TargetRenderers)
			{
				ProGifManager.Instance.m_GifPlayer.AddExtraDestination(renderer);
			}
		}
    }

    public void RemoveFromExtraDestination()
	{
		if(m_IsMultiGifPlayer)
		{
			if(_lastPGifRenderer == null || PGif.iGetPlayer(_lastPGifRenderer.name) == null)
			{
				return;
			}

			foreach(Renderer renderer in m_TargetRenderers)
			{
				PGif.iGetPlayer(_lastPGifRenderer.name).RemoveFromExtraDestination(renderer);
			}
		}
		else
		{
			if(ProGifManager.Instance.m_GifPlayer == null)
			{
				return;
			}

			foreach(Renderer renderer in m_TargetRenderers)
			{
				ProGifManager.Instance.m_GifPlayer.RemoveFromExtraDestination(renderer);
			}
		}
    }

}