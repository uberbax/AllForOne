using UnityEngine;
using UnityEngine.UI;

public class GUIMarketWindow : MonoBehaviour
{
    public WhoHeroesObjectRef building = new WhoHeroesObjectRef();
    public GUIInfoItem general;
    public Button back;
    private RObj runtime;

    private void Start()
    {
        back?.onClick.AddListener(() => gameObject.SetActive(false));
        EventManager.SUB(WhoHeroesEvents.Refresh, _ => { if (gameObject.activeInHierarchy) Fill(); });
    }

    public void Fill()
    {
        runtime = GUILIB.Resolve(building, gameObject);
        if (runtime != null) general?.Fill(runtime);
    }

    private void OnEnable()
    {
        Fill();
    }
}
