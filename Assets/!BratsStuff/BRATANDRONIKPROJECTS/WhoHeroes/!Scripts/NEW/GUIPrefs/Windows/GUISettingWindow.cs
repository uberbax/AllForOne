using UnityEngine;
using UnityEngine.SceneManagement;
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
        restart?.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));
        quit?.onClick.AddListener(Application.Quit);
        settings?.SetUp();
        EventManager.SUB(WhoHeroesEvents.Refresh, _ => Fill());
    }

    private void OnEnable()
    {
        Fill();
    }

    public void Fill()
    {
        settings?.Fill();
    }
}
