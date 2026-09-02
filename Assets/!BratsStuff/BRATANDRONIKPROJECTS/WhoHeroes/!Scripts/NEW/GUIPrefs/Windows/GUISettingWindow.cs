using UnityEngine;
using UnityEngine.UI;

public class GUISettingWindow : MonoBehaviour
{
    public GUISettings settings;
    public Button back;
    public Button quit;
    public Button reset;
    public Button restart;

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
        quit?.onClick.AddListener(Application.Quit);
        settings?.SetUp();
        EventManager.SUB(WhoHeroesEvents.Refresh, OnRefresh);
    }

    private void OnEnable()
    {
        Fill();
    }

    public void Fill()
    {
        settings?.Fill();
    }

    private void OnRefresh(ArgPass _)
    {
        Fill();
    }

    private void OnDestroy()
    {
        EventManager.UNSUB(WhoHeroesEvents.Refresh, OnRefresh);
    }
}
