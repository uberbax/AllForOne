using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using System.Linq;

public class MultipleCameraDemo : MonoBehaviour
{
    public Camera mCamera1;
    public Camera mCamera2;
    public Camera mCamera3;

    public Text cam1Text;
    public Text cam2Text;
    public Text cam3Text;

    public Image image1;
    public Image image2;
    public Image image3;

//#if PRO_GIF_GUITEXTURE
//    public GUITexture m_GuiTexture;
//#endif
    public MeshRenderer m_CubeMesh;

    private Texture2D _refTexture2d, _refImageTexture2d;

    // Use this for initialization
    void Start()
    {
        //Change setting before camera1 start recording
        //PGif.iSetRecordSettings(true, 300, 300, 3, 24, 0, 30);
        PGif.iSetRecordSettings(true, 480, 480, 5, 10, 0, 30);
        //PGif.iSetRecordSettings(new Vector2(1, 1), 300, 300, 5, 10, 1, 30);
        //Start recording with camera1
        PGif.iStartRecord(mCamera1, "Cam1", OnRecordProgress1, OnRecordDurationMax1, OnPreProcessingDone1, OnFileSaveProgress1, OnFileSaved1, autoClear: false);
        cam1Text.text = "Camera1 started recording";

        //Change setting before camera2 start recording
        PGif.iSetRecordSettings(new Vector2(1, 1), 300, 300, 5, 15, 1, 30);
        //Start recording with camera2
        PGif.iStartRecord(mCamera2, "Cam2", OnRecordProgress2, OnRecordDurationMax2, OnPreProcessingDone2, OnFileSaveProgress2, OnFileSaved2, autoClear: false);
        cam2Text.text = "Camera2 started recording";

        //Change setting before camera3 start recording
        PGif.iSetRecordSettings(new Vector2(4, 3), 200, 200, 7, 20, 1, 30);
        //Start recording with camera3
        PGif.iStartRecord(mCamera3, "Cam3", OnRecordProgress3, OnRecordDurationMax3, OnPreProcessingDone3, OnFileSaveProgress3, OnFileSaved3, autoClear: false);
        cam3Text.text = "Camera3 started recording";

//#if PRO_GIF_GUITEXTURE
//        m_GuiTexture.gameObject.SetActive(true);
//#endif
    }


    public void OnRecordProgress1(float progress)
    {
        //Debug.Log("Cam1 - [MultipleCameraDemo] On record progress: " + progress);
    }

    public void OnRecordDurationMax1()
    {
        Debug.Log("Cam1 - [MultipleCameraDemo] On recorder buffer max.");
        cam1Text.text = "Camera1 duration Max";
    }

    public void OnPreProcessingDone1()
    {
        Debug.Log("Cam1 - [MultipleCameraDemo] On pre-processing done.");
        cam1Text.text = "Camera1 pre-processing done";

        // Resume(Restart) the recorder for storing new textures from camera
        PGif.iResumeRecord("Cam1");
        Debug.Log("Resume the recorder: Cam1");

    }

    public void OnFileSaveProgress1(int id, float progress)
    {
        //Debug.Log("Cam1 - [MultipleCameraDemo] Save progress: " + progress);
        cam1Text.text = "Camera1 Save progress: " + progress;
    }

    public void OnFileSaved1(int id, string path)
    {
        Debug.Log("Cam1 - [MultipleCameraDemo] On saved, path: " + path);
        cam1Text.text = "Camera1 Saved: " + path;

        //Preview the GIF. Do not clear/autoClear the recorder if you want to preview the GIF from a recorder source, you can clear the recorder after you quit from the preview
        //_PlayGif("Cam1", "GifPlayer1", image1, ProGifRecorderComponent.EncodePlayMode.Normal);

        //PGif.iClearRecorder("Cam1");
    }


    public void OnRecordProgress2(float progress)
    {
        //Debug.Log("Cam2 - [MultipleCameraDemo] On record progress: " + progress);
    }

    public void OnRecordDurationMax2()
    {
        Debug.Log("Cam2 - [MultipleCameraDemo] On recorder buffer max.");
        cam2Text.text = "Camera2 duration Max";
    }

    public void OnPreProcessingDone2()
    {
        Debug.Log("Cam2 - [MultipleCameraDemo] On pre-processing done.");
        cam2Text.text = "Camera2 pre-processing done";

        // Resume(Restart) the recorder for storing new textures from camera
        PGif.iResumeRecord("Cam2");
        Debug.Log("Resume the recorder: Cam2");
    }

    public void OnFileSaveProgress2(int id, float progress)
    {
        //Debug.Log("Cam2 - [MultipleCameraDemo] Save progress: " + progress);
        cam2Text.text = "Camera2 Save progress: " + progress;
    }

    public void OnFileSaved2(int id, string path)
    {
        Debug.Log("Cam2 - [MultipleCameraDemo] On saved, path: " + path);
        cam2Text.text = "Camera3 Saved: " + path;

        //Preview the GIF. Do not clear/autoClear the recorder if you want to preview the GIF from a recorder source, you can clear the recorder after you quit from the preview
        //_PlayGif("Cam2", "GifPlayer2", image2, ProGifRecorderComponent.EncodePlayMode.Normal);
    }


    public void OnRecordProgress3(float progress)
    {
        //Debug.Log("Cam3 - [MultipleCameraDemo] On record progress: " + progress);
    }

    public void OnRecordDurationMax3()
    {
        Debug.Log("Cam3 - [MultipleCameraDemo] On recorder buffer max.");
        cam3Text.text = "Camera3 duration Max";
    }

    public void OnPreProcessingDone3()
    {
        Debug.Log("Cam3 - [MultipleCameraDemo] On pre-processing done.");
        cam3Text.text = "Camera3 pre-processing done";

        // Resume(Restart) the recorder for storing new textures from camera
        PGif.iResumeRecord("Cam3");
        Debug.Log("Resume the recorder: Cam3");
    }

    public void OnFileSaveProgress3(int id, float progress)
    {
        //Debug.Log("Cam3 - [MultipleCameraDemo] Save progress: " + progress);
        cam3Text.text = "Camera3 Save progress: " + progress;
    }

    public void OnFileSaved3(int id, string path)
    {
        Debug.Log("Cam3 - [MultipleCameraDemo] On saved, path: " + path);
        cam3Text.text = "Camera3 Saved: " + path;

        //Preview the GIF. Do not clear/autoClear the recorder if you want to preview the GIF from a recorder source, you can clear the recorder after you quit from the preview
        //_PlayGif("Cam3", "GifPlayer3", image3, ProGifRecorderComponent.EncodePlayMode.Normal);

        //Make use of OnPlaying callback to display gif on extra target object that supports Texture2D/Sprite.
//        PGif.iGetPlayer("GifPlayer3").SetOnPlayingCallback((getGTex) => {
//#if PRO_GIF_GUITEXTURE
//            //			if(m_GuiTexture != null) m_GuiTexture.texture = getGTex.GetTexture2D();
//            //			if(m_CubeMesh != null) m_CubeMesh.material.mainTexture = getGTex.GetTexture2D();
//            getGTex.SetDisplay(m_GuiTexture, ref _refTexture2d);
//#endif
        //});
    }

    private void _PlayGif(string recorderName, string playerName, Image destination, ProGifRecorderComponent.EncodePlayMode encodePlayMode)
    {
        if (PGif.iGetRecorder(recorderName) == null || PGif.iGetRecorder(recorderName).Frames == null)
        {
            Debug.LogWarning("The recorder not exist or has been cleared: " + recorderName);
            return;
        }

        PGif.iPlayGif(PGif.iGetRecorder(recorderName), destination, playerName, (progress) =>
        {
            //Set display size
            float gifRatio = (float)PGif.iGetRecorder(recorderName).Width / (float)PGif.iGetRecorder(recorderName).Height;
            _SetDisplaySize(gifRatio, destination);

            // If you want to set the (player) Play Mode, make sure all textures are imported/loaded to the player first. Just check the loading progress equals 1 (100%).
            if (progress >= 1)
            {
                switch (encodePlayMode)
                {
                    case ProGifRecorderComponent.EncodePlayMode.Normal:
                        break;
                    case ProGifRecorderComponent.EncodePlayMode.PingPong:
                        PGif.iGetPlayer(playerName).PingPong();
                        break;
                    case ProGifRecorderComponent.EncodePlayMode.Reverse:
                        PGif.iGetPlayer(playerName).Reverse();
                        break;
                }
            }
        });

        //PGif.iClearRecorder_Delay(recorderName, playerName, (recorder)=> {
        //    Debug.Log(recorder + " is cleared.");

        //    PGif.iStartRecord(mCamera1, recorder, OnRecordProgress1, OnRecordDurationMax1, OnPreProcessingDone1, OnFileSaveProgress1, OnFileSaved1, autoClear: false);
        //    cam1Text.text = recorder + " started recording";
        //});
    }

    private void _SetDisplaySize(float gifWHRatio, Image destination)
    {
        int maxDisplayWidth = (int)destination.rectTransform.sizeDelta.x;
        int maxDisplayHeight = (int)destination.rectTransform.sizeDelta.y;

        int displayWidth = maxDisplayWidth;
        int displayHeight = maxDisplayHeight;
        if (gifWHRatio > 1f)
        {
            displayWidth = maxDisplayWidth;
            displayHeight = (int)((float)displayWidth / gifWHRatio);
        }
        else if (gifWHRatio < 1f)
        {
            displayHeight = maxDisplayHeight;
            displayWidth = (int)((float)displayHeight * gifWHRatio);
        }
        destination.rectTransform.sizeDelta = new Vector2(displayWidth, displayHeight);
    }

    #region ---- UI Control ----
    public void SaveRecord_Cam1()
    {
        // Change the waiting interval for the GIF frames. 
        //PGif.iGetRecorder("Cam1").SetOverrideFrameDelay(1f/30);

        // Set the play mode for encoding the GIF file.
        PGif.iGetRecorder("Cam1").recorderCom.m_EncodePlayMode = ProGifRecorderComponent.EncodePlayMode.Normal;

        //PGif.iStopAndSaveRecord("Cam1");
        PGif.iSaveRecord("Cam1");   // You can resume the recorder if the Stop() method is never called.
        Debug.Log("Save the recorder: Cam1");

        //Preview the GIF immediately
        _PlayGif("Cam1", "GifPlayer1", image1, ProGifRecorderComponent.EncodePlayMode.Normal);
    }

    public void SaveRecord_Cam2()
    {
        // Change the waiting interval for the GIF frames. 
        //PGif.iGetRecorder("Cam2").SetOverrideFrameDelay(1f/30);

        // Set the play mode for encoding the GIF file.
        PGif.iGetRecorder("Cam2").recorderCom.m_EncodePlayMode = ProGifRecorderComponent.EncodePlayMode.PingPong;

        //PGif.iStopAndSaveRecord("Cam2");
        PGif.iSaveRecord("Cam2"); // You can resume the recorder if the Stop() method is never called.
        Debug.Log("Save the recorder: Cam2");

        //Preview the GIF immediately
        _PlayGif("Cam2", "GifPlayer2", image2, ProGifRecorderComponent.EncodePlayMode.PingPong);
    }

    public void SaveRecord_Cam3()
    {
        // Change the waiting interval for the GIF frames. 
        //PGif.iGetRecorder("Cam3").SetOverrideFrameDelay(1f/30);

        // Set the play mode for encoding the GIF file.
        PGif.iGetRecorder("Cam3").recorderCom.m_EncodePlayMode = ProGifRecorderComponent.EncodePlayMode.Reverse;

        //PGif.iStopAndSaveRecord("Cam3");
        PGif.iSaveRecord("Cam3"); // You can resume the recorder if the Stop() method is never called.
        Debug.Log("Save the recorder: Cam3");

        //Preview the GIF immediately
        _PlayGif("Cam3", "GifPlayer3", image3, ProGifRecorderComponent.EncodePlayMode.Reverse);
    }

    int _counter = 0;
    public void UpdateCubeText(TextMesh tm)
    {
        _counter++;
        if (_counter > 9) _counter = 0;
        tm.text = _counter.ToString();
    }
    #endregion

    #region ---- Extra Use Cases ----
    public void FastPreviewAndSaveGif_WithCombinedRecorders()
    {
        // Combine textures of Cam1 & Cam2 recorders: add the Cam2 textures to Cam1
        PGif.iStopRecord("Cam1");
        PGif.iStopRecord("Cam2");

        RenderTexture[] renderTextures1 = PGif.iGetRecorder("Cam2").Frames;
        ProGifRecorderComponent proGifRecoderCom = PGif.iGetRecorder("Cam1").recorderCom;
        for (int i = 0; i < renderTextures1.Length; i++)
        {
            proGifRecoderCom.Frames.Enqueue(renderTextures1[i]);
        }

        Image previewImage = image1;
        // Preview the textures in the recorder directly
        PGif.iPlayGif(PGif.iGetRecorder("Cam1"), previewImage, "GifPreviewPlayer", (progress) => {
            //Set display size
            float gifRatio = (float)PGif.iGetRecorder("Cam1").Width / (float)PGif.iGetRecorder("Cam1").Height;
            _SetDisplaySize(gifRatio, previewImage);
        });

        SaveRecord_Cam1();
    }

    public void SaveRecord_CombineCam1AndCam2()
    {
        // Combine textures of Cam1 & Cam2 recorders: add the Cam2 textures to Cam1
        PGif.iStopRecord("Cam2");
        RenderTexture[] renderTextures1 = PGif.iGetRecorder("Cam2").Frames;
        ProGifRecorderComponent proGifRecoderCom = PGif.iGetRecorder("Cam1").recorderCom;
        for (int i = 0; i < renderTextures1.Length; i++)
        {
            proGifRecoderCom.Frames.Enqueue(renderTextures1[i]);
        }

        // Save the Cam1 recorder
        PGif.iStopAndSaveRecord("Cam1");
    }

    #endregion

}
