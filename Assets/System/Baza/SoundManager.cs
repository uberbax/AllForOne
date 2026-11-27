using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Add the listener before EventSystem processes the release at execution order -1000.
[DefaultExecutionOrder(-1100)]
public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;
    public AudioClip click;
    [SerializeField] private bool playButtonClicks;
    private readonly List<Button> pendingClickButtons = new List<Button>();
    private readonly List<RaycastResult> buttonHits = new List<RaycastResult>();
    private PointerEventData buttonPointer;
    private EventSystem buttonEventSystem;

    public AudioSource source;
    private void Awake()
    {
        instance = this;
    }

    private RObj mon;
    void Start()
    {
        if (!MainStates.instance.all.ContainsKey("settings"))
        {
            Invoke("Start", 0.1f);
            return;
        }
        Debug.Log("SOUND");
        mon = MainStates.instance.all["settings"];
    }

    private void Update()
    {
        if (!playButtonClicks || click == null) return;
        var eventSystem = EventSystem.current;
        if (eventSystem == null || !(eventSystem.currentInputModule is StandaloneInputModule module)) return;

        var input = module.input;
        int touchCount = input.touchCount;
        for (int i = 0; i < touchCount; i++)
        {
            var touch = input.GetTouch(i);
            if (touch.type != TouchType.Indirect && touch.phase == TouchPhase.Ended)
                ArmButtonClick(eventSystem, touch.position, touch.fingerId);
        }
        if (touchCount == 0 && input.mousePresent && input.GetMouseButtonUp(0))
            ArmButtonClick(eventSystem, input.mousePosition, -1);
    }

    private void ArmButtonClick(EventSystem eventSystem, Vector2 position, int pointerId)
    {
        if (buttonEventSystem != eventSystem)
        {
            buttonEventSystem = eventSystem;
            buttonPointer = new PointerEventData(eventSystem);
        }
        buttonPointer.Reset();
        buttonPointer.position = position;
        buttonPointer.pointerId = pointerId;
        buttonPointer.button = PointerEventData.InputButton.Left;
        eventSystem.RaycastAll(buttonPointer, buttonHits);
        var hit = buttonHits.Count > 0 ? buttonHits[0].gameObject : null;
        buttonHits.Clear();
        var handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit);
        if (handler == null || !handler.TryGetComponent<Button>(out var button) ||
            !button.IsInteractable() || pendingClickButtons.Contains(button)) return;

        // Let the EventSystem decide whether the release is a click or a cancelled drag.
        button.onClick.AddListener(PlayClick);
        pendingClickButtons.Add(button);
    }

    private void LateUpdate() => ClearButtonClickListeners();

    private void OnDisable() => ClearButtonClickListeners();

    private void ClearButtonClickListeners()
    {
        foreach (var button in pendingClickButtons)
            if (button != null) button.onClick.RemoveListener(PlayClick);
        pendingClickButtons.Clear();
    }

    public void PlayClick() => PlayEffect(click);

    public void PlayEffect(AudioClip clip)
    {
        if (clip == null || mon == null) return;

        float volume = Mathf.Clamp01(mon.GetPar("volume_sound"));
        if (volume <= 0f) return;

        var listenerCamera = Camera.main;
        AudioSource.PlayClipAtPoint(clip, listenerCamera != null ? listenerCamera.transform.position : transform.position, volume);
    }

    public void PlayAny(string sound, int spellType = 0)
    {
        if (mon == null) return;
        
        if (ResourceHolder.instance.sounds.ContainsKey(sound))
        {
            AudioSource.PlayClipAtPoint(ResourceHolder.instance.sounds[sound], Camera.main.transform.position, mon.GetPar("volume_sound"));
        }
        else if (spellType == 1)
        {
            Debug.Log(mon);
            AudioSource.PlayClipAtPoint(ResourceHolder.instance.sounds["buff"], Camera.main.transform.position, mon.GetPar("volume_sound"));
        }
        else if (spellType == 2)
        {
            AudioSource.PlayClipAtPoint(ResourceHolder.instance.sounds["spell"], Camera.main.transform.position, mon.GetPar("volume_sound"));
        }
    }

    public void PlayMusic()
    {
        
    }
    
    
    
}
