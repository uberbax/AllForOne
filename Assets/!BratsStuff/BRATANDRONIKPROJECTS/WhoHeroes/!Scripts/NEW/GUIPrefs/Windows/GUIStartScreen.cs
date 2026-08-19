using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUIStartScreen : MonoBehaviour
{
    public bool startScreenOff;
    [SerializeField] private string steamAppUrl = "https://store.steampowered.com/app/4633390/DeadZone_Outpost/";
    public Button start;
    public Button wish;
    public Button quit;
    public List<UnoLoc> forseTranslate = new List<UnoLoc>();

    private void Awake()
    {
        startScreenOff |= PlayerPrefs.GetInt("restart", 0) == 1;
        PlayerPrefs.SetInt("restart", 1);
        var canvas = GetComponentInChildren<Canvas>(true);
        if (canvas != null) canvas.enabled = !startScreenOff;
    }

    private void Start()
    {
        start?.onClick.AddListener(() => EventManager.INV("game_start", new ArgPass()));
        wish?.onClick.AddListener(() => Application.OpenURL(steamAppUrl));
        quit?.onClick.AddListener(Application.Quit);
        if (startScreenOff && start != null)
            FunctionTimer.Create(() => start.onClick.Invoke(), 0.01f);
    }
}
