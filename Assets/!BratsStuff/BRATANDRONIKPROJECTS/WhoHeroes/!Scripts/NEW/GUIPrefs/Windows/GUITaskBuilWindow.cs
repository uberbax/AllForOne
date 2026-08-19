using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUITaskBuilWindow : MonoBehaviour
{
    public WhoHeroesObjectRef building = new WhoHeroesObjectRef();
    public GUIInfoItem general;
    public Button back;
    public GUIUnitShort storyOwner;
    public Button viewStory;
    public TextMeshProUGUI story;
    public TextMeshProUGUI storyprogress;
    private RObj runtime;

    private void Start()
    {
        back?.onClick.AddListener(() => gameObject.SetActive(false));
        viewStory?.onClick.AddListener(OpenStory);
        EventManager.SUB(WhoHeroesEvents.Refresh, _ => { if (gameObject.activeInHierarchy) Fill(); });
    }

    private void OpenStory()
    {
        var id = GUILIB.StringParam(runtime, "story");
        if (Dialoguer.instance != null && ConfigLoader.Instance.dictDialogues.ContainsKey(id))
            Dialoguer.instance.ShowDialogue(id);
        else
            GUILIB.Emit(WhoHeroesEvents.Dialogue, runtime, id);
    }

    public void Fill(RObj value = null)
    {
        runtime = value ?? GUILIB.Resolve(building, gameObject);
        if (runtime == null) return;
        general?.Fill(runtime, "dbuildingstory");
        var storyId = GUILIB.StringParam(runtime, "story");
        if (story != null) GUILIB.Instance.Translate(story, storyId);
        if (storyprogress != null) storyprogress.text = Mathf.RoundToInt(runtime.GetPar("story_stage") + 1) + "/6";
        storyOwner?.Fill(runtime.inventory.FirstOrDefault(x => x.it == ItemType.monster));
    }
}
