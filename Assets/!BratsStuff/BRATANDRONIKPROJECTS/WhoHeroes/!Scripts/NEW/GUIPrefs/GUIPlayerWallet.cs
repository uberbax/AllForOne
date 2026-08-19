using UnityEngine;

public class GUIPlayerWallet : MonoBehaviour
{
    public GUIResources wallet;

    private void Start()
    {
        EventManager.SUB(WhoHeroesEvents.Refresh, _ => { if (gameObject.activeInHierarchy) Fill(); });
        Fill();
    }

    public void Fill(System.Collections.Generic.List<Bon> resources = null)
    {
        wallet?.Fill(resources ?? GUILIB.AsResources(GUILIB.PlayerInventory()));
    }

    private void OnEnable()
    {
        Fill();
    }
}
