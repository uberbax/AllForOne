using UnityEngine;

public class WindowSlider : MonoBehaviour
{
   [Header("Window Setup")]
    public string wtype = "";
    public RectTransform window;         
    public Transform shownPos;           
    public Transform hiddenPos;          

    [Header("Settings")]
    public bool isVertical = false;      
    public string hotKey = "Tab";        
    public float speed = 10f;            

    private bool isOpen = false;        
    private Vector3 targetPos;           
    private Vector3 shownLayoutOffset;
    private Vector3 hiddenLayoutOffset;

    public bool IsOpen => isOpen;

    [Header("Input Lock")]
    public BRATViewMapCameraController cameraInput;

    void Start()
    {
        RefreshTarget();
        window.position = targetPos;
        ApplyCameraInputLock();
    }

    void Update()
    {
        
        if (Input.GetKeyDown(hotKey))
            Toggle();

        
        window.position = Vector3.Lerp(window.position, targetPos, Time.deltaTime * speed);
    }

   
    public void Toggle()
    {
        SetOpen(!isOpen);
    }

    
    public void Open()
    {
        SetOpen(true);
    }

   
    public void Hide()
    {
        SetOpen(false);
    }

    private void SetOpen(bool value)
    {
        if (isOpen == value)
            return;
        isOpen = value;
        RefreshTarget();
        ApplyCameraInputLock();
        EventManager.INV("slide_window", new ArgPass { what = wtype, num = isOpen ? 1 : 0 });
    }

    public void SetVerticalWindowSizeDelta(float heightDelta)
    {
        shownLayoutOffset = Vector3.up * (heightDelta * 0.5f);
        hiddenLayoutOffset = Vector3.down * (heightDelta * 0.5f);
        RefreshTarget();
    }

    private void OnDisable()
    {
        if (!isOpen)
            return;
        isOpen = false;
        RefreshTarget();
        cameraInput?.SetInputBlocked(false);
        EventManager.INV("slide_window", new ArgPass { what = wtype, num = 0 });
    }

    private void RefreshTarget()
    {
        var target = isOpen ? shownPos : hiddenPos;
        if (target == null)
            return;
        targetPos = target.position + (isOpen ? shownLayoutOffset : hiddenLayoutOffset);
    }

    private void ApplyCameraInputLock()
    {
        cameraInput?.SetInputBlocked(isOpen);
    }
}
