using UnityEngine;
using UnityEngine.UI;

public class GUISettingWindow : MonoBehaviour
{
    public GUISettings settings;
    public Button back;
    public Button quit;
    public Button reset;
    public Button restart;

    private float previousTimeScale = 1f;
    private bool pausedByWindow;

    private void Start()
    {
        SetUp();
        Fill();
    }

    public void SetUp()
    {
        back?.onClick.AddListener(() => gameObject.SetActive(false));
        reset?.onClick.AddListener(() => EventManager.INV(WhoHeroesEvents.ResetRequested, new ArgPass()));
        restart?.onClick.AddListener(() => EventManager.INV(WhoHeroesEvents.RestartRequested, new ArgPass()));
        quit?.onClick.AddListener(SaveAndQuit);
        settings?.SetUp();
        EventManager.SUB(WhoHeroesEvents.Refresh, OnRefresh);
    }

    private void OnEnable()
    {
        if (Application.isPlaying && !pausedByWindow)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            pausedByWindow = true;
        }
        Fill();
    }

    private void OnDisable()
    {
        if (!pausedByWindow)
            return;
        Time.timeScale = previousTimeScale;
        pausedByWindow = false;
    }

    public void Fill()
    {
        settings?.Fill();
    }

    private void OnRefresh(ArgPass _)
    {
        Fill();
    }

    public void SaveAndQuit()
    {
        MainCycle_WhoHeroes.Instance?.SaveNow();
        Application.Quit();
    }

    private void OnDestroy()
    {
        EventManager.UNSUB(WhoHeroesEvents.Refresh, OnRefresh);
    }
}
