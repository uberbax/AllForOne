// Created by SwanDEV 2017

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ProGifDemoMgr : MonoBehaviour
{
    #region ----- Prefabs -----
    public GameObject prefab_GifControlPanel;
    public GameObject prefab_GifPreviewAndSharePanel;
    public GameObject prefab_GifPlayerPanel;

    public static T InstantiatePrefab<T>(GameObject prefab) where T : MonoBehaviour
    {
        if (prefab != null)
        {
            GameObject go = GameObject.Instantiate(prefab) as GameObject;
            if (go != null)
            {
                go.name = "[Prefab]" + prefab.name;
                go.transform.localScale = Vector3.one;
                return go.GetComponent<T>();
            }
            else
            {
                Debug.Log("prefab is null!");
                return null;
            }
        }
        else
            return null;
    }
    #endregion

    public Transform componentContainerT;
    public Camera m_MainCamera;
    public CanvasScaler m_MainCanvasScaler;

    public TextMesh m_TM_Counter;
    public MeshRenderer m_CubeMesh;
    public RawImage m_RawImage;
    //public Image m_Image;

    public DImageDisplayHandler m_ImageDisplayHandler;

    private Texture2D _refTexture2d;

    private static ProGifDemoMgr _instance;
    public static ProGifDemoMgr Instance
    {
        get
        {
            return _instance;
        }
    }

    void Start()
    {
        _instance = this;

        if (m_AntiAliasingLevel > 0) QualitySettings.antiAliasing = m_AntiAliasingLevel;

        SetButtonState(btn_PauseRecord, ProGifManager.GetColor(ProGifManager.CommonColorEnum.Black), false);
        SetButtonState(btn_ResumeRecord, ProGifManager.GetColor(ProGifManager.CommonColorEnum.Black), false);
        SetButtonState(btn_SaveRecord, ProGifManager.GetColor(ProGifManager.CommonColorEnum.Black), false);
        SetButtonState(btn_CancelRecord, ProGifManager.GetColor(ProGifManager.CommonColorEnum.Black), false);

        //Check screen orientation for setting canvas resolution
        if (Screen.width > Screen.height)
        {
            m_MainCanvasScaler.referenceResolution = new Vector2(1920, 1080);
        }
        else
        {
            m_MainCanvasScaler.referenceResolution = new Vector2(1080, 1920);
        }
    }

    public void ToggleCanvasMode(Text buttonText)
    {
        Canvas canvas = m_MainCanvasScaler.GetComponent<Canvas>();
        canvas.renderMode = canvas.renderMode == RenderMode.ScreenSpaceCamera ? RenderMode.ScreenSpaceOverlay : RenderMode.ScreenSpaceCamera;
        buttonText.text = "Toggle Canvas Mode\n" + (canvas.renderMode == RenderMode.ScreenSpaceCamera ? "(Show UI)" : "(Hide UI)");
    }

    int counter = 0;
    public void AddCounter()
    {
        if (m_TM_Counter == null) return;

        counter++;
        if (counter > 9) counter = 0;
        m_TM_Counter.text = counter.ToString();
    }

    #region ----- GIF Recording -----
    private float nextBubbleMessageTime = 0f;
    private ProGifControlPanel m_ProGifPanel = null;

    public Button btn_ShowGifPanel;
    public Button btn_PauseRecord;
    public Button btn_ResumeRecord;
    public Button btn_SaveRecord;
    public Button btn_CancelRecord;
    public Button btn_ShowGifPlayerPanel;

    public Slider sld_Progress;
    public Text text_Progress;

    [Space]
    [Tooltip("Set the anti-aliasing level in the QualitySettings; 1 = OFF, 2 = 2x 4x, 8x")]
    public int m_AntiAliasingLevel = 0;


    public void UpdateRecordOrSaveProgress(float progress)
    {
        if (progress > 1f) progress = 1f;
        if (sld_Progress != null) sld_Progress.value = progress;
        if (text_Progress != null) text_Progress.text = "Progress: " + (int)(100 * progress) + " %";

        if (ProGifManager.Instance.m_GifRecorder != null && ProGifManager.Instance.m_GifRecorder.State == ProGifRecorder.RecorderState.Recording)
        {
            SetButtonState(btn_PauseRecord, ProGifManager.GetColor(ProGifManager.CommonColorEnum.Blue), true);
            SetButtonState(btn_ResumeRecord, ProGifManager.GetColor(ProGifManager.CommonColorEnum.Blue), true);
            SetButtonState(btn_SaveRecord, ProGifManager.GetColor(ProGifManager.CommonColorEnum.Blue), true);
            SetButtonState(btn_CancelRecord, ProGifManager.GetColor(ProGifManager.CommonColorEnum.Blue), true);

            //Preview
            RenderTexture getTex = ProGifManager.Instance.m_GifRecorder.GetTexture();
            if (m_CubeMesh) m_CubeMesh.material.mainTexture = getTex;
            if (m_ImageDisplayHandler && m_RawImage && getTex) m_ImageDisplayHandler.SetRawImage(m_RawImage, getTex);
        }
    }

    public void SetGifProgressColor(Color color)
    {
        if (sld_Progress.fillRect.GetComponent<UnityEngine.UI.Image>().color != color)
        {
            sld_Progress.fillRect.GetComponent<UnityEngine.UI.Image>().color = color;
            //Debug.Log("SetGifProgressColor: " + color.ToString());
        }
    }

    public void ShowGIFPanel()
    {
        if (ProGifManager.Instance.m_GifRecorder != null)
        {
            if (Time.time > nextBubbleMessageTime)
            {
                nextBubbleMessageTime = Time.time + 2f;

                if (ProGifManager.Instance.m_GifRecorder.State == ProGifRecorder.RecorderState.Paused)
                {
                    //Encoding all stored frames into a GIF file 
                    Debug.Log("Making GIF, please wait");
                }
                else if (ProGifManager.Instance.m_GifRecorder.State == ProGifRecorder.RecorderState.Recording)
                {
                    SaveRecord();
                }
            }
        }
        else
        {
            m_ProGifPanel = ProGifControlPanel.Create(prefab_GifControlPanel, componentContainerT);
            m_ProGifPanel.Setup(() =>
            {
                //Update UI
                SetGifProgressColor(ProGifManager.GetColor(ProGifManager.CommonColorEnum.LightYellow));

                SetButtonState(btn_ShowGifPlayerPanel, ProGifManager.GetColor(ProGifManager.CommonColorEnum.Black), false);

                SetButtonState(btn_SaveRecord, ProGifManager.GetColor(ProGifManager.CommonColorEnum.Blue), true);
                SetButtonState(btn_CancelRecord, ProGifManager.GetColor(ProGifManager.CommonColorEnum.Blue), true);

            }, UpdateRecordOrSaveProgress, () =>
            {
                Debug.Log("DemoMgr - Record duration MAX.");
            });
        }
    }

    public void PauseRecord()
    {
        Debug.Log("Pause Recording");
        ProGifManager.Instance.PauseRecord();
    }

    public void ResumeRecord()
    {
        Debug.Log("Resume Recording");
        ProGifManager.Instance.ResumeRecord();
    }

    public void SaveRecord()
    {
        // Preview the GIF immediately
        ShowGifPreviewAndSharePanel("", loadFile: false);
        // Btw, the simplest way to play/preview the GIF is calling the PlayGif method directly, like the commented example below:
        //ProGifManager.Instance.PlayGif(m_RawImage, (progress) => {});

        Debug.Log("Start saving GIF");
        ProGifManager.Instance.StopAndSaveRecord(
            () => {
                Debug.Log("On recorder pre-processing done.");
            },

            (id, progress) => {
                UpdateRecordOrSaveProgress(progress);
                SetGifProgressColor(ProGifManager.GetColor(ProGifManager.CommonColorEnum.Red));
            },

            (id, path) =>
            {
                // Update UI
                SetButtonState(btn_ShowGifPanel, ProGifManager.GetColor(ProGifManager.CommonColorEnum.Blue), true);
                SetButtonState(btn_ShowGifPlayerPanel, ProGifManager.GetColor(ProGifManager.CommonColorEnum.Blue), true);
                SetButtonState(btn_SaveRecord, ProGifManager.GetColor(ProGifManager.CommonColorEnum.Black), false);
                SetButtonState(btn_CancelRecord, ProGifManager.GetColor(ProGifManager.CommonColorEnum.Black), false);
                UpdateRecordOrSaveProgress(1f);

#if SDEV_MobileMedia
                // Save to Native Gallery (Native Save Path available on Android only)
                string androidNativeSavePath = MobileMedia.CopyMedia(path, "ProGif_Recorder", System.IO.Path.GetFileNameWithoutExtension(path), ".gif", isImage: true);
                Debug.Log("Mobile Media Save Path: " + androidNativeSavePath);
#endif

                StartCoroutine(_OnFileSaved());
            }
        );

        // Disable buttons
        SetButtonState(btn_PauseRecord, ProGifManager.GetColor(ProGifManager.CommonColorEnum.Black), false);
        SetButtonState(btn_ResumeRecord, ProGifManager.GetColor(ProGifManager.CommonColorEnum.Black), false);
        SetButtonState(btn_SaveRecord, ProGifManager.GetColor(ProGifManager.CommonColorEnum.Black), false);
    }

    public void CancelRecord()
    {
        Debug.Log("Cancel Recording");
        ProGifManager.Instance.StopRecord();
        ProGifManager.Instance.ClearRecorder();

        SetButtonState(btn_PauseRecord, ProGifManager.GetColor(ProGifManager.CommonColorEnum.Black), false);
        SetButtonState(btn_ResumeRecord, ProGifManager.GetColor(ProGifManager.CommonColorEnum.Black), false);
        SetButtonState(btn_SaveRecord, ProGifManager.GetColor(ProGifManager.CommonColorEnum.Black), false);
        SetButtonState(btn_CancelRecord, ProGifManager.GetColor(ProGifManager.CommonColorEnum.Black), false);
        SetButtonState(btn_ShowGifPlayerPanel, ProGifManager.GetColor(ProGifManager.CommonColorEnum.Blue), true);

        _ResetGifProgress();
    }

    private IEnumerator _OnFileSaved()
    {
        yield return new WaitForSeconds(2f);
        _ResetGifProgress();
    }

    private void _ResetGifProgress()
    {
        UpdateRecordOrSaveProgress(0f);
        SetGifProgressColor(ProGifManager.GetColor(ProGifManager.CommonColorEnum.White));
    }

    private IEnumerator _OnLoadingComplete()
    {
        yield return new WaitForSeconds(2f);
        _ResetGifProgress();
    }

    public void ShowGifPreviewAndSharePanel(string gifPath, bool loadFile)
    {
        // Create the preview panel from prefab to show the textures stored in the recorder.
        ProGifPreviewSharePanel gifPreview = ProGifPreviewSharePanel.Create(prefab_GifPreviewAndSharePanel, componentContainerT);
        gifPreview.Setup(gifPath, loadFile, (progress) => {

            Debug.Log("progress: " + (int)(progress * 100) + " %");

            // Show progress on UI 
            UpdateRecordOrSaveProgress(progress);
            SetGifProgressColor(ProGifManager.GetColor(ProGifManager.CommonColorEnum.Green));

            // Check progress
            if (progress >= 1f)
            {
                StartCoroutine(_OnLoadingComplete());
            }
        });

        // Display on different targets
        ProGifManager.Instance.SetPlayerOnFirstFrame((frame) => {
            // Get the Texture2D reference from the first playback target
            Texture2D texture2D = gifPreview.m_GifImage.sprite.texture;

            // Set the texture references for other targets
            m_CubeMesh.material.mainTexture = texture2D;
            m_RawImage.texture = texture2D;
            if (m_ImageDisplayHandler && m_RawImage) m_ImageDisplayHandler.SetRawImage(m_RawImage, frame.width, frame.height);
        });
    }

    private void PlayGifPingPong_ProGifManager()
    {
        ProGifManager.Instance.m_GifPlayer.SetOnPlayingCallback((gifTex) =>
        {
            ProGifPlayerComponent playerComponent = ProGifManager.Instance.m_GifPlayer.playerComponent;
            if (playerComponent != null)
            {
                int currentSpriteIndex = playerComponent.spriteIndex;
                if (currentSpriteIndex == 0)
                {
                    playerComponent.gifTextures.Reverse();
                    Debug.Log("Reverse");
                }
            }
        });
    }

    private void PlayGifPingPong_PGif()
    {
        ProGifPlayer proGifPlayer = PGif.iGetPlayer("YourGifPlayerName");
        if (proGifPlayer != null)
        {
            proGifPlayer.SetOnPlayingCallback((gifTex) =>
            {
                ProGifPlayerComponent playerComponent = proGifPlayer.playerComponent;
                if (playerComponent != null)
                {
                    int currentSpriteIndex = playerComponent.spriteIndex;
                    if (currentSpriteIndex == 0)
                    {
                        playerComponent.gifTextures.Reverse();
                        Debug.Log("Reverse");
                    }
                }
            });
        }
    }

    public void ShowPlayerPanel(string gifPath)
    {
        ProGifPlayerPanel playerPanel = ProGifPlayerPanel.Create(prefab_GifPlayerPanel, componentContainerT);
        playerPanel.Setup(gifPath, (progress) =>
        {
            UpdateRecordOrSaveProgress(progress);
            SetGifProgressColor(ProGifManager.GetColor(ProGifManager.CommonColorEnum.Green));

            //Check progress
            if (progress >= 1f)
            {
                StartCoroutine(_OnLoadingComplete());
            }
        });
    }

    public void SetButtonState(Button button, Color color, bool enable)
    {
        button.enabled = enable;
        button.image.color = color;
    }

    public void SetTransparentExtras()
    {
        ProGifManager.Instance.m_GifRecorder.SetTransparentExtras(true, ProGifManager.Instance.m_Width, ProGifManager.Instance.m_Height);
    }

    public void UpdateTransparentColor()
    {
        ProGifManager.Instance.m_GifRecorder.SetTransparent(ProGifManager.Instance.m_TransparentColor, 0);
    }

#endregion

}
