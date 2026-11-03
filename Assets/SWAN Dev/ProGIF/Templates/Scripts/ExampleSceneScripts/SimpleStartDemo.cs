using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleStartDemo : MonoBehaviour
{
	public TextMesh textGameState;
	public TextMesh textGifState;

	public float gameTimingToStopRecord = 12f;
	bool gameEnd = false;

	public Camera mCamera = null;

    [Space()]
    /// <summary>
    /// The recorder will save gif using this filename if this is provided. The new gif will replace the old one if their filename are the same.
    /// </summary>
    [Tooltip("The recorder will save gif using this filename if this is provided. The new gif will replace the old one if their filename are the same.")]
    public string optionalGifFileName = "MyGif";

    [Header("Native Save (+MobileMediaPlugin)")]
    public bool saveToNative = false;
    public bool deleteOriginGif = false;
    public string folderName = "GIF Demo";


	// Use this for initialization
	void Start()
	{
		//Create an instance for ProGifManager
		ProGifManager gifMgr = ProGifManager.Instance;

		//Make some changes to the record settings, you can let it auto aspect with screen size.. 
		gifMgr.SetRecordSettings(true, 300, 300, 3, 15, 1, 30);
		//Or give an aspect ratio for cropping gif frames just before encoding
		//gifMgr.SetRecordSettings(new Vector2(1, 1), 300, 300, 1, 15, 0, 30);

		//Start gif recording
		gifMgr.StartRecord((mCamera != null)? mCamera:Camera.main,
			(progress)=>{
				Debug.Log("[SimpleStartDemo] On record progress: " + progress);
			},
			()=>{
				Debug.Log("[SimpleStartDemo] On recorder buffer max.");
			});

		textGameState.text = "Game Started";
		textGifState.text = "Start Record..";
	}

	float nextUpdateTime = 0f;
	void Update()
	{
		if(gameEnd) return;

		if(Time.time > nextUpdateTime)
		{
			//nextUpdateTime = Time.time + 0.5f;
			Camera.main.backgroundColor = new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));
		}

		if(Time.time > gameTimingToStopRecord - 1f)
		{
			textGameState.text = "Game Over";
		}

		if(Time.time > gameTimingToStopRecord)
		{
			gameEnd = true;
			ProGifManager gifMgr = ProGifManager.Instance;

			//Stop the recording
			gifMgr.StopAndSaveRecord(
				()=>{
					Debug.Log("[SimpleStartDemo] On pre-processing done.");
				}, 
				(id, progress)=>{
					//Debug.Log("[SimpleStartDemo] On save progress: " + progress);
					if(progress < 1f)
					{
						textGifState.text = "Making Gif : " + Mathf.CeilToInt(progress * 100) + "%";
					}
					else
					{
						textGifState.text = "The gif file is created, find the path in the debug console.";
					}
				},
				(id, path)=>{
					//Clear the existing recorder, player and reset gif manager
					gifMgr.Clear();
                    Debug.Log("[SimpleStartDemo] On saved, origin save path: " + path);

					if (saveToNative)
					{
#if SDEV_MobileMedia
						// Copy the newly created GIF file from internal path to external, i.e. Android/iOS device gallery
                        string nativeSavePath = MobileMedia.CopyMedia(path, folderName, System.IO.Path.GetFileNameWithoutExtension(path), ".gif", true);
                        /*MobileMedia.SaveBytes(System.IO.File.ReadAllBytes(path), "YourGifFolderName", "YourGifFileName", ".gif", true);*/


                        if (deleteOriginGif) System.IO.File.Delete(path);

                        Debug.Log("Native Save Path(Andorid Only): " + nativeSavePath);
#if UNITY_EDITOR
                        Application.OpenURL(System.IO.Path.GetDirectoryName(nativeSavePath));
#endif

#endif
					}
					else
					{
#if UNITY_EDITOR
						Application.OpenURL(System.IO.Path.GetDirectoryName(path));
#endif
					}
                },
                optionalGifFileName
                );
		}
	}
}