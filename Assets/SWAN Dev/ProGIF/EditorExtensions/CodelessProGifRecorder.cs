// Created by SwanDEV 2018

using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Pro GIF codeless GIF recorder tool: recording GIF using any specific camera in the scene.
/// [ HOW To USE ] Attach this script on a GameObject in the scene, or drag the CodelessProGifRecorder prefab from the EditorExtensions folder to the scene.
/// </summary>
public class CodelessProGifRecorder : MonoBehaviour
{
    [Header("[ Save Settings ]")]
    public FilePathName.AppPath m_SaveDirectory = FilePathName.AppPath.PersistentDataPath;
    public string m_FolderName;
    [Tooltip("Optional filename without extension. (Will auto generate a filename base on date and time if this not provided)")]
    public string m_OptionalFileName;
    public string Rec_FolderName { get { return m_FolderName; } set { m_FolderName = value; } }
    public string Rec_OptionalFileName { get { return m_OptionalFileName; } set { m_OptionalFileName = value; } }

#if SDEV_MobileMedia
    [Tooltip("Copy the GIF to the External directory, the External directory can be accessed in the Android Gallery or iOS Photos (Android & iOS only)")]
#else
    [Tooltip("Copy the GIF to the External directory, the External directory can be accessed in the Android Gallery or iOS Photos (Android & iOS only)" +
        " *** MobileMedia Plugin(MMP) required. When you have the plugin installed, " +
        "add the scripting define symbol ''SDEV_MobileMedia'' in the Unity PlayerSettings > OtherSettings > Scripting Define Symbols")]
#endif
    public bool m_SaveExternal = false;
#if SDEV_MobileMedia
    public bool m_DeleteOriginGifAfterSaveExternal = true;
#endif

    [Header("[ Recorder Settings ]")]
	[Tooltip("If 'True', automatically save the recorder when Record Progress is 100%.")]
	public bool m_AutoSave = false;
    public bool Rec_AutoSave { get { return m_AutoSave; } set { m_AutoSave = value; } }

	[Space]
    [Tooltip("The target aspect ratio for cropping GIF. **Leave this value Zero if you want it to Auto determine the aspect ratio with the screen.")]
	public Vector2 m_AspectRatio = new Vector2(0, 0);
    public float Rec_AspectWidth { get { return m_AspectRatio.x; } set { m_AspectRatio.x = value; } }
    public float Rec_AspectHeight { get { return m_AspectRatio.y; } set { m_AspectRatio.y = value; } }
    [Tooltip("Auto determines the aspect ratio with the screen if this is 'true'.")]
	public bool m_AutoAspect = true;
    public bool Rec_AutoAspect { get { return m_AutoAspect; } set { m_AutoAspect = value; } }
    public int m_Width = 360;
    public float Rec_Width { get { return m_Width; } set { m_Width = (int)value; } }
    public int m_Height = 360;
    public float Rec_Height { get { return m_Height; } set { m_Height = (int)value; } }
    [Tooltip("The target recording time in seconds.")]
	public float m_Duration = 3f;
    public float Rec_Duration { get { return m_Duration; } set { m_Duration = value; } }
    [Tooltip("The GIF target framerate (frames per second).")]
	[Range(1, 60)] public int m_Fps = 15;
    public float Rec_Fps { get { return m_Fps; } set { m_Fps = (int)value; } }
    [Tooltip("Loop time for the GIF. -1: no repeat, 0: loop forever, or set any number greater than 0.")]
	public int m_Loop = 0;								//-1: no repeat, 0: infinite, >0: repeat count
    public float Rec_Loop { get { return m_Loop; } set { m_Loop = (int)value; } }
    [Tooltip("The quality of GIF range from 1 to 100, 1 is the best quality but the slowest, 10 - 30 has a good balance.")]
	[Range(1, 100)] public int m_Quality = 20;			//(1 - 100), 1: best(larger storage size), 100: faster(smaller storage size)
    public float Rec_Quality { get { return m_Quality; } set { m_Quality = (int)value; } }

    [Space]
    public Color32 m_TransparentColor;
    public float Rec_TransparentColorR_Byte { get { return m_TransparentColor.r; } set { m_TransparentColor.r = (byte)value; } }
    public float Rec_TransparentColorG_Byte { get { return m_TransparentColor.g; } set { m_TransparentColor.g = (byte)value; } }
    public float Rec_TransparentColorB_Byte { get { return m_TransparentColor.b; } set { m_TransparentColor.b = (byte)value; } }
    public float Rec_TransparentColorA_Byte { get { return m_TransparentColor.a; } set { m_TransparentColor.a = (byte)value; } }
    public byte m_TransparentColorRange = 0;
    public float Rec_TransparentColorRange_Byte { get { return m_TransparentColorRange; } set { m_TransparentColorRange = (byte)value; } }

    [Header("[ Hotkeys & GUI ]")]
    public bool m_EnableHotkeys = true;
    public KeyCode m_Hotkey_StartRec = KeyCode.R;
    public KeyCode m_Hotkey_Save = KeyCode.S;
    public KeyCode m_Hotkey_Pause = KeyCode.P;
    public KeyCode m_Hotkey_Resume = KeyCode.O;
    public KeyCode m_Hotkey_Cancel = KeyCode.Escape;
    public bool m_ShowGUI = true;
    [Tooltip("Position and size of the GUI text and buttons. Change the 'H' value for changing the button and text size.")]
    public Rect m_GUIRect = new Rect(0.01f, 0.01f, 0.5f, 0.05f);
    public Color m_GUITextColor = Color.green;
    
	[Header("[ Camera Settings ]")]
	[Tooltip("The camera for recording GIF. Drag camera on this variable or click the 'Find Camera' button to setup.")]
	public Camera m_RecorderCamera;
	public Camera[] m_AllCameras;
	private int _currCameraIndex = 0;
	private const string _recorderName = "CodelessProGifRecorder";

	[HideInInspector] public string m_RecordingProgress = "0%";
    [HideInInspector] public string m_SaveProgress = "0%";
    [HideInInspector] public string m_State = "Idle";
	[HideInInspector] [TextArea(1, 2)] public string m_SavePath = "GIF Path";
    [HideInInspector] public bool m_RecorderStarted;
    
    private Rect _GUIRect = new Rect();

    private void OnGUI()
    {
        if (m_EnableHotkeys)
        {
            if (Input.GetKey(m_Hotkey_StartRec) && State == ProGifRecorder.RecorderState.Stopped)
            {
                StartRecord();
            }
            if (Input.GetKey(m_Hotkey_Save))
            {
                SaveRecord();
            }
            if (Input.GetKey(m_Hotkey_Pause))
            {
                PauseRecord();
            }
            if (Input.GetKey(m_Hotkey_Resume))
            {
                ResumeRecord();
            }
            if (Input.GetKey(m_Hotkey_Cancel))
            {
                CancelRecord();
            }
        }

        if (m_ShowGUI)
        {
            _GUIRect.x = m_GUIRect.x * Screen.width;
            _GUIRect.y = m_GUIRect.y * Screen.height;
            _GUIRect.width = m_GUIRect.width * Screen.width;
            _GUIRect.height = m_GUIRect.height * Screen.height;

            GUIStyle guiStyle = new GUIStyle()
            {
                fontSize = (int)_GUIRect.height - 4,
                normal = new GUIStyleState() { textColor = Color.green }
            };
            guiStyle.fontSize = Mathf.Max(8, (int)_GUIRect.height - 4);
            guiStyle.normal.textColor = m_GUITextColor;

            GUI.Label(_GUIRect, "ProGIF Editor Recorder: " + m_RecordingProgress + "[" + m_State + "]", guiStyle);
            
            GUIStyle btnStyle = new GUIStyle("button");
            btnStyle.fontSize = Mathf.Max(8, (int)_GUIRect.height - 8);
            float btnPosX = _GUIRect.x;
            float btnWidth = m_Hotkey_StartRec.ToString().Length * _GUIRect.height / 2f + _GUIRect.height;
            Rect btnRect = new Rect(btnPosX, _GUIRect.y + _GUIRect.height * 1.5f, btnWidth, _GUIRect.height * 1.5f);
            if (GUI.Button(btnRect, m_Hotkey_StartRec.ToString(), btnStyle) && State == ProGifRecorder.RecorderState.Stopped)
            {
                StartRecord();
            }
            btnPosX += btnWidth + 4;

            btnWidth = m_Hotkey_Save.ToString().Length * _GUIRect.height / 2f + _GUIRect.height;
            btnRect.x = btnPosX;
            btnRect.width = btnWidth;
            if (GUI.Button(btnRect, m_Hotkey_Save.ToString(), btnStyle))
            {
                SaveRecord();
            }
            btnPosX += btnWidth + 4;

            btnWidth = m_Hotkey_Pause.ToString().Length * _GUIRect.height / 2f + _GUIRect.height;
            btnRect.x = btnPosX;
            btnRect.width = btnWidth;
            if (GUI.Button(btnRect, m_Hotkey_Pause.ToString(), btnStyle))
            {
                PauseRecord();
            }
            btnPosX += btnWidth + 4;

            btnWidth = m_Hotkey_Resume.ToString().Length * _GUIRect.height / 2f + _GUIRect.height;
            btnRect.x = btnPosX;
            btnRect.width = btnWidth;
            if (GUI.Button(btnRect, m_Hotkey_Resume.ToString(), btnStyle))
            {
                ResumeRecord();
            }
            btnPosX += btnWidth + 4;

            btnWidth = m_Hotkey_Cancel.ToString().Length * _GUIRect.height / 2f + _GUIRect.height;
            btnRect.x = btnPosX;
            btnRect.width = btnWidth;
            if (GUI.Button(btnRect, m_Hotkey_Cancel.ToString(), btnStyle))
            {
                CancelRecord();
            }
        }
    }

    public void FindCameras()
	{
		m_AllCameras = Camera.allCameras;

		if(m_AllCameras != null && m_AllCameras.Length > 0 && m_RecorderCamera == null)
		{
			m_RecorderCamera = m_AllCameras[0];
		}
	}

	public void SetCamera(int index)
	{
        if (_currCameraIndex == index || m_AllCameras == null || m_AllCameras.Length == 0) return;
        _currCameraIndex = Mathf.Clamp(index, 0, m_AllCameras.Length - 1);
        if (_currCameraIndex < m_AllCameras.Length) m_RecorderCamera = m_AllCameras[_currCameraIndex];
    }

	public void StartRecord()
	{
        if (!Application.isPlaying) return;

        m_RecorderStarted = true;
		m_State = "Recording..";
        if (!m_AutoAspect && m_AspectRatio.x > 0f && m_AspectRatio.y > 0f)
        {
            PGif.iSetRecordSettings(m_AspectRatio, m_Width, m_Height, m_Duration, m_Fps, m_Loop, m_Quality);
        }
        else
        {
            PGif.iSetRecordSettings(m_AutoAspect, m_Width, m_Height, m_Duration, m_Fps, m_Loop, m_Quality);
        }
        PGif.iSetTransparent(m_TransparentColor, m_TransparentColorRange);
        PGif.iStartRecord(((m_RecorderCamera == null)?Camera.main:m_RecorderCamera), _recorderName, 
			(progress)=>{
                m_RecordingProgress = Mathf.CeilToInt(progress * 100) + "%";
            },
			()=>{
				m_State = "Press the <Save Record> button to save GIF";
                if (m_AutoSave) SaveRecord();
			},
			()=> {
                // do nothing
            },
			(id, progress)=>{
                m_SaveProgress = Mathf.CeilToInt(progress * 100) + "%";
            },
			(id, path)=>{
                m_SavePath = path;
                m_RecordingProgress = "0%";
                m_SaveProgress = "0%";
                m_State = "Idle";

#if SDEV_MobileMedia
                if (m_SaveExternal)
                {
                    string fileName = FilePathName.Instance.GetFileNameWithoutExt();
                    string folderName = string.IsNullOrEmpty(m_FolderName.Trim()) ? Application.productName : m_FolderName;
                    string externalSavePath = MobileMedia.CopyMedia(path, folderName, fileName, ".gif", true);
                    if (m_DeleteOriginGifAfterSaveExternal)
                    {
                        m_SavePath = externalSavePath;
                        File.Delete(path);
                    }
                }
                
                if (!m_DeleteOriginGifAfterSaveExternal)
#endif
                {
                    // Check to Move the file to the specified location
                    FilePathName fpn = FilePathName.Instance;
                    string toPath = fpn.GetAppPath(m_SaveDirectory);
                    if (!string.IsNullOrEmpty(m_FolderName)) toPath = Path.Combine(toPath, m_FolderName);
                    if (!string.IsNullOrEmpty(m_OptionalFileName))
                    {
                        toPath = Path.Combine(toPath, m_OptionalFileName);
                        if (!toPath.ToLower().EndsWith(".gif")) toPath += ".gif";
                    }
                    else
                    {
                        toPath = Path.Combine(toPath, Path.GetFileName(path));
                    }
                    if (Path.GetDirectoryName(path) != Path.GetDirectoryName(toPath))
                    {
                        fpn.MoveFile(path, toPath, replaceIfExist: true);
                        m_SavePath = toPath;
                    }
                }
            }
		);
	}

	public void SaveRecord()
	{
        if (!Application.isPlaying) return;

        if (m_RecorderStarted && State != ProGifRecorder.RecorderState.Stopped)
        {
            m_RecorderStarted = false;
            m_State = "Saving..";
            PGif.iStopAndSaveRecord(_recorderName);
        }
    }

    public void PauseRecord()
    {
        if (!Application.isPlaying) return;

        m_State = "Paused";
        PGif.iPauseRecord(_recorderName);
    }

    public void ResumeRecord()
    {
        if (!Application.isPlaying) return;

        m_State = "Recording..";
        PGif.iResumeRecord(_recorderName);
    }

    public void CancelRecord()
    {
        if (!Application.isPlaying) return;

        m_RecorderStarted = false;
        m_RecordingProgress = "0%";
        m_SaveProgress = "0%";
        m_State = "Idle";
        PGif.iStopRecord(_recorderName);
        PGif.iClearRecorder(_recorderName);
    }

    public ProGifRecorder.RecorderState State
    {
        get
        {
            if (!PGif.HasInstance || PGif.iGetRecorder(_recorderName) == null) return ProGifRecorder.RecorderState.Stopped;
            return PGif.iGetRecorder(_recorderName).State;
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(CodelessProGifRecorder))]
public class CodelessProGifRecorderCustomEditor : Editor
{
    private static string[] cameraOptions = new string[] { };
    private static int cameraSelection = 0;

    private static bool showHelpsMessage = false;
    private string helpsMessage = "[ HOW ] Attach this script on a GameObject in the scene, modify the recorder settings and start recording GIF."
        + "\n\n---- Steps (For Editor) ----"
        + "\n(1) Run your scene, click the 'Find Camera' button and set camera."
        + "\n(2) Click the 'Start Record' button to start the recorder."
        + "\n(3) Click the 'Save Record' button to save the recorded frames as GIF."
        + "\n(4) Wait until the 'Save Progress' become 100%."
        + "\n\n\n---- For App at Runtime ----"
        + "\nYou can also reference the methods and dynamic parameters in the CodelessProGifRecorder to your UI components like Button, Slider, InputField, and Toggle, etc. in the scene, this allows you to record GIFs at runtime in your app."
        + "\n* This codeless tool provides a convenient way for recording GIFs without changing your codes, which has its limitations. Please integrate the API of PGif manager into your scripts, if you need some more advanced features.";

	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();
        
        bool isLightSkin = !EditorGUIUtility.isProSkin;

        GUIStyle helpBoxStyle = new GUIStyle(EditorStyles.textArea);

        //GUI.backgroundColor = isLightSkin ? new Color(0.8f, 0.7f, 0.2f, 1f) : Color.yellow; 

        CodelessProGifRecorder recorder = (CodelessProGifRecorder)target;
        
        cameraSelection = GUILayout.SelectionGrid(cameraSelection, cameraOptions, 2);
		recorder.SetCamera(cameraSelection);

		GUILayout.Label("\nFind all Cameras in the scene:\n(Drag the camera you want to Recorder Camera)");
		if(GUILayout.Button("Find Cameras"))
		{
            _SetupCamera(recorder);
		}

        if (recorder.State == ProGifRecorder.RecorderState.Stopped)
        {
            GUILayout.Label("\n\n[ Start Record GIF ]\nStart record GIF with Recorder Camera, or main camera:");
            if (GUILayout.Button("Start Record") && Application.isPlaying)
            {
                _SetupCamera(recorder);
                recorder.StartRecord();
            }
        }
        else
        {
            GUILayout.Label("\n\n[ Recorder Started ]\nControl & Save the recorder using below buttons:");
            EditorGUILayout.BeginHorizontal();
            if (recorder.State == ProGifRecorder.RecorderState.Recording)
            {
                if (GUILayout.Button("Pause Record")) recorder.PauseRecord();
            }
            else
            {
                if (GUILayout.Button("Resume Record")) recorder.ResumeRecord();
            }

            if (GUILayout.Button("Cancel Record")) recorder.CancelRecord();
            EditorGUILayout.EndHorizontal();
        }

        GUILayout.Label("Stop and save the stored frames as GIF:");
        if (GUILayout.Button("Save Record"))
        {
            recorder.SaveRecord();
        }

		GUILayout.Label("\nRecord Progress: " + recorder.m_RecordingProgress
			+ "\nSave Progress: " + recorder.m_SaveProgress
			+ "\nStatus: " + recorder.m_State
			+ "\n\nGIF Path: " + recorder.m_SavePath + "\n", helpBoxStyle);

		if(GUILayout.Button("View GIF"))
		{
			if(string.IsNullOrEmpty(recorder.m_SavePath)) return;
			_OpenURL(FilePathName.Instance.EnsureLocalPath(recorder.m_SavePath));
		}

		if(GUILayout.Button("Reveal In Folder"))
		{
			if(string.IsNullOrEmpty(recorder.m_SavePath)) return;
			string fileName = Path.GetFileName(recorder.m_SavePath);
			string directoryPath = recorder.m_SavePath.Remove(recorder.m_SavePath.IndexOf(fileName));
			_OpenURL(FilePathName.Instance.EnsureLocalPath(directoryPath));
		}

		if(GUILayout.Button("Copy GIF Path"))
		{
			if(string.IsNullOrEmpty(recorder.m_SavePath)) return;

			TextEditor te = null;
			te = new TextEditor();
			te.text = recorder.m_SavePath;
			te.SelectAll();
			te.Copy();
		}

        GUILayout.Space(10);

        Color tipTextColor = isLightSkin ? new Color(0.12f, 0.12f, 0.12f, 1f) : Color.cyan;
        helpBoxStyle.normal.textColor = tipTextColor;

        GUIStyle tipsStyle = new GUIStyle(EditorStyles.boldLabel);
        tipsStyle.normal.textColor = tipTextColor;

        showHelpsMessage = GUILayout.Toggle(showHelpsMessage, " Help (How To Use? Click here...)", tipsStyle);
        if (showHelpsMessage) GUILayout.Label(helpsMessage, helpBoxStyle);

        GUILayout.Space(10);
        if (GUILayout.Button("Write A Review (THANK YOU)"))
        {
            _OpenURL("https://www.swanob2.com/progif");
        }
    }

    private void _OpenURL(string url)
    {
#if UNITY_EDITOR_OSX
        System.Diagnostics.Process.Start(url);
#else
        Application.OpenURL(url);
#endif
    }

    private void _SetupCamera(CodelessProGifRecorder recorder)
    {
        recorder.FindCameras();
        _SetCameraOptions(Camera.allCameras, recorder);
    }

    private void _SetCameraOptions(Camera[] cameras, CodelessProGifRecorder recorder)
	{
        cameraSelection = 0;
        cameraOptions = new string[cameras.Length];
		for(int i=0; i<cameras.Length; i++)
		{
			cameraOptions[i] = cameras[i].name;
            if (recorder.m_RecorderCamera && recorder.m_RecorderCamera == cameras[i]) cameraSelection = i;
        }
	}
}
#endif
