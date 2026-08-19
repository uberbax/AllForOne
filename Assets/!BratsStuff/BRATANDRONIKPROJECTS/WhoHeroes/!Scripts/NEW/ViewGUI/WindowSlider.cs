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

    void Start()
    {
        
        targetPos = isOpen ? shownPos.position : hiddenPos.position;
        window.position = targetPos;
    }

    void Update()
    {
        
        if (Input.GetKeyDown(hotKey))
            Toggle();

        
        window.position = Vector3.Lerp(window.position, targetPos, Time.deltaTime * speed);
    }

   
    public void Toggle()
    {
        isOpen = !isOpen;
        targetPos = isOpen ? shownPos.position : hiddenPos.position;

        EventManager.INV("slide_window", new ArgPass { what = wtype, num = isOpen ? 1 : 0 });
    }

    
    public void Open()
    {
        isOpen = true;
        targetPos = shownPos.position;
    }

   
    public void Hide()
    {
        isOpen = false;
        targetPos = hiddenPos.position;
    }
}
