
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using UnityEngine.UI;

public class GifSplashScreen : MonoBehaviour
{
    [Tooltip("")]
    public string m_GifFileName = "Logo.gif";
    public string m_NextSceneName = "Main";
    [Range(0f, 1f)] public float m_DecodeProgressToStartPlayer = 0.5f;
    public bool m_OptimizeMemoryUsage = false;

    [Header("[ UI Components ]")]
    public RawImage m_RawImage;
    public Image m_ImageCover;
    public Slider m_Slider_LoadingProgress;

    private void Start()
    {
        if (!m_GifFileName.ToLower().EndsWith(".gif")) m_GifFileName += ".gif";

        string gifPath = Path.Combine(Application.streamingAssetsPath, m_GifFileName);

        ProGifManager.Instance.SetPlayerOptimization(m_OptimizeMemoryUsage);

        int totalFrame = 0;
        ProGifManager.Instance.PlayGif(gifPath, m_RawImage, (progress) =>
        {
            // Update the UI slider using the progress value.
            m_Slider_LoadingProgress.value = progress;

            if (progress >= m_DecodeProgressToStartPlayer)
            {
                ProGifManager.Instance.ResumePlayer();
            }
        }, false);

        // Pause the gif after the first frame is displayed, wait for the decoder to finish decode.
        ProGifManager.Instance.SetPlayerOnFirstFrame((result) =>
        {
            m_ImageCover.gameObject.SetActive(false);

            DImageDisplayHandler imageDisplayHandler = m_RawImage.GetComponent<DImageDisplayHandler>();
            if (imageDisplayHandler) imageDisplayHandler.SetRawImage(m_RawImage, result.width, result.height);

            totalFrame = result.totalFrame;
            if (m_DecodeProgressToStartPlayer > 0f)
            {
                ProGifManager.Instance.PausePlayer();
            }
        });

        // Start to play the gif when decode finished.
        ProGifManager.Instance.SetPlayerOnDecodeComplete((result) =>
        {
            ProGifManager.Instance.ResumePlayer();
            m_Slider_LoadingProgress.gameObject.SetActive(false);
        });

        // Use the OnPlaying callback, and check the current frame index until the last frame, ensure the entire gif is played.
        // Then load the Main scene.
        ProGifManager.Instance.SetPlayerOnPlaying((gifTexture) =>
        {

            if (ProGifManager.Instance.m_GifPlayer.playerComponent.spriteIndex == totalFrame - 1)
            {
                // Stop the player when all the frames played.
                ProGifManager.Instance.StopPlayer();

                // Wait 1 update frame to avoid the set pixel error in the player.
                SDemoAnimation.Instance.WaitFrames(1, () =>
                {
                    ProGifManager.Instance.Clear();
                    _MoveScene();
                });
            }

        });
    }

    private void _MoveScene()
    {
        if(!string.IsNullOrEmpty(m_NextSceneName)) SceneManager.LoadScene(m_NextSceneName);
    }

}
