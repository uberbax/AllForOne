using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PartyCampController : MonoBehaviour
{
    [SerializeField] private GameObject environment;
    [SerializeField] private Camera campCamera;
    [SerializeField] private Canvas campCanvas;
    [SerializeField] private RectTransform safeArea, design, slotOverlay, rosterContent;
    [SerializeField] private PartyCampHeroCard cardTemplate;
    [SerializeField] private PartyCampSlot[] slots;
    [SerializeField] private Button backButton, autoAssignButton, clearButton;
    [SerializeField] private TMP_Text countLabel, statusLabel, emptyLabel;
    [SerializeField] private bool pauseWorld = true;
    private readonly List<PartyCampHeroCard> cards = new List<PartyCampHeroCard>();
    private readonly List<RObj> heroes = new List<RObj>();
    private readonly List<Camera> savedCameras = new List<Camera>();
    private readonly List<bool> cameraFlags = new List<bool>();
    private readonly List<Canvas> savedCanvases = new List<Canvas>();
    private readonly List<bool> canvasFlags = new List<bool>();
    private readonly List<GraphicRaycaster> savedRaycasters = new List<GraphicRaycaster>();
    private readonly List<MobileJoystick> savedJoysticks = new List<MobileJoystick>();
    private float previousTimeScale, nextRefresh;
    private bool open, changing;
    private string previousCameraTag;
    private Rect lastSafeArea;
    private Vector2Int lastScreen;
    public bool IsOpen => open;
    public Camera CampCamera => campCamera;
    private void Awake()
    {
        backButton.onClick.AddListener(Hide);
        autoAssignButton.onClick.AddListener(AutoAssign);
        clearButton.onClick.AddListener(ClearParty);
        environment.SetActive(false); campCanvas.gameObject.SetActive(false); campCamera.enabled = false;
    }
    public void Show()
    {
        if (open || MainStates.instance == null || !MainStates.instance.HasMain()) return;
        savedCameras.Clear(); cameraFlags.Clear(); savedCanvases.Clear(); canvasFlags.Clear();
        savedRaycasters.Clear(); savedJoysticks.Clear();
        open = true; previousTimeScale = Time.timeScale; previousCameraTag = campCamera.tag;
        foreach (var camera in FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (camera == campCamera || camera.gameObject.scene != gameObject.scene) continue;
            savedCameras.Add(camera); cameraFlags.Add(camera.enabled); camera.enabled = false;
        }
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (canvas == campCanvas || canvas.transform.IsChildOf(campCanvas.transform) ||
                canvas.gameObject.scene != gameObject.scene || canvas.GetComponentInParent<FPSCounter>() != null) continue;
            savedCanvases.Add(canvas); canvasFlags.Add(canvas.enabled); canvas.enabled = false;
            var raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null && raycaster.enabled) { savedRaycasters.Add(raycaster); raycaster.enabled = false; }
        }
        foreach (var joystick in FindObjectsByType<MobileJoystick>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (joystick.enabled) { savedJoysticks.Add(joystick); joystick.enabled = false; }
        if (pauseWorld) Time.timeScale = 0;
        environment.SetActive(true); campCamera.tag = "MainCamera"; campCamera.enabled = true; campCanvas.gameObject.SetActive(true);
        ApplySafeArea(); Refresh(); nextRefresh = Time.unscaledTime + .25f;
    }
    public void Hide() => Close(true);

    private void Close(bool refreshUi)
    {
        if (!open) return;
        open = false;
        if (campCamera != null) { campCamera.enabled = false; campCamera.tag = previousCameraTag; }
        if (campCanvas != null) campCanvas.gameObject.SetActive(false);
        if (environment != null) environment.SetActive(false);
        for (int i=0;i<savedCameras.Count;i++) if (savedCameras[i]!=null) savedCameras[i].enabled=cameraFlags[i];
        for (int i=0;i<savedCanvases.Count;i++) if (savedCanvases[i]!=null) savedCanvases[i].enabled=canvasFlags[i];
        foreach(var raycaster in savedRaycasters) if(raycaster!=null)raycaster.enabled=true;
        foreach(var joystick in savedJoysticks) if(joystick!=null)joystick.enabled=true;
        if (pauseWorld) Time.timeScale = previousTimeScale;
        savedCameras.Clear(); cameraFlags.Clear(); savedCanvases.Clear(); canvasFlags.Clear(); savedRaycasters.Clear(); savedJoysticks.Clear();
        if (refreshUi) UIfiller.GlobalRefresh();
    }
    private void Update()
    {
        if (!open) return;
        if (Input.GetKeyDown(KeyCode.Escape)) { Hide(); return; }
        ApplySafeArea();
        if (Time.unscaledTime >= nextRefresh) { nextRefresh = Time.unscaledTime + .25f; Refresh(); }
        foreach (var slot in slots) slot.Project(campCamera, slotOverlay);
    }
    private void Refresh()
    {
        var state = MainStates.instance;
        if (state == null || !state.HasMain()) { Close(false); return; }
        var player = state.mainPlayer;
        heroes.Clear();
        foreach (var item in player.inventory) if (PartyCampRoster.IsHero(item)) heroes.Add(item);
        while (cards.Count < heroes.Count)
        {
            var card = Instantiate(cardTemplate, rosterContent); card.name = "HeroCard_" + cards.Count;
            cards.Add(card);
        }
        for (int i=0;i<cards.Count;i++)
        {
            if (i>=heroes.Count) { cards[i].gameObject.SetActive(false); continue; }
            cards[i].Bind(this, heroes[i], PartyCampRoster.IsAssigned(heroes[i],state.petsLoIndex,state.petsHiIndex));
            cards[i].gameObject.SetActive(true);
        }
        int assigned = 0, capacity = Mathf.Max(0,state.petsHiIndex-state.petsLoIndex);
        for(int i=0;i<slots.Length;i++)
        {
            RObj hero = null;
            foreach(var item in heroes) if(item.GetPar("used_slot")==state.petsLoIndex+i) {hero=item;break;}
            if(hero!=null)assigned++;
            slots[i].Bind(this,hero,i); slots[i].gameObject.SetActive(i<capacity);
        }
        countLabel.SetText("{0}/{1}",assigned,capacity);
        emptyLabel.gameObject.SetActive(heroes.Count==0);
        autoAssignButton.interactable=heroes.Count>0; clearButton.interactable=assigned>0;
    }
    public void ToggleHero(RObj hero)
    {
        if(!open || changing)return;
        changing=true;
        try
        {
            if(PartyCampRoster.Toggle(MainStates.instance,hero,out var message)) Commit();
            statusLabel.text=message;
        }
        finally{changing=false;Refresh();}
    }
    public void AutoAssign()
    {
        if(!open||changing)return;changing=true;
        try { if(PartyCampRoster.AutoAssign(MainStates.instance)){Commit();statusLabel.text="Party assigned by rarity, ascension and level";} }
        finally{changing=false;Refresh();}
    }
    public void ClearParty()
    {
        if(!open||changing)return;changing=true;
        try
        {
            var state=MainStates.instance;
            if(state==null||!state.HasMain())return;
            foreach(var hero in state.mainPlayer.inventory)
                if(PartyCampRoster.IsHero(hero)&&hero.GetPar("used_slot")>=0)hero.SetPar("used_slot",-1);
            Commit();statusLabel.text="Party cleared";
        }
        finally{changing=false;Refresh();}
    }
    private void Commit()
    {
        if(PartyController.instance!=null)PartyController.instance.RefreshParty();
        if(MainStates.instance.useSave)MainStates.instance.Save();
    }
    private void ApplySafeArea()
    {
        var screen=new Vector2Int(Screen.width,Screen.height);var safe=Screen.safeArea;
        if(screen==lastScreen&&safe==lastSafeArea)return;
        if(screen.x<=0||screen.y<=0)return;
        lastScreen=screen;lastSafeArea=safe;
        safeArea.anchorMin=new Vector2(safe.xMin/screen.x,safe.yMin/screen.y);
        safeArea.anchorMax=new Vector2(safe.xMax/screen.x,safe.yMax/screen.y);
        safeArea.offsetMin=safeArea.offsetMax=Vector2.zero;
        float width=safeArea.rect.width;
        design.localScale=Vector3.one*Mathf.Min(width/1080f,safeArea.rect.height/2400f);
    }
    private void OnDisable(){if(open)Close(false);}
    private void OnDestroy()
    {
        if(open)Close(false);
        if(backButton!=null)backButton.onClick.RemoveListener(Hide);
        if(autoAssignButton!=null)autoAssignButton.onClick.RemoveListener(AutoAssign);
        if(clearButton!=null)clearButton.onClick.RemoveListener(ClearParty);
    }
}
