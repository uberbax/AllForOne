using System.Collections.Generic;
using UnityEngine;

public class GUIPlayerWallet : MonoBehaviour
{
    private static readonly string[] ResourceOrder =
    {
        MainCycle_WhoHeroes.GoldResourceId,
        MainCycle_WhoHeroes.WoodResourceId,
        MainCycle_WhoHeroes.StoneResourceId
    };

    public GUIResources wallet;

    private void Start()
    {
        EventManager.SUB(WhoHeroesEvents.Refresh, OnRefresh);
        Fill();
    }

    private void OnRefresh(ArgPass _)
    {
        if (gameObject.activeInHierarchy)
            Fill();
    }

    private void OnDestroy()
    {
        EventManager.UNSUB(WhoHeroesEvents.Refresh, OnRefresh);
    }

    public void Fill(List<Bon> resources = null)
    {
        if (wallet == null)
            return;

        resources ??= GUILIB.AsResources(GUILIB.PlayerInventory());

        var valuesById = new Dictionary<string, int>(System.StringComparer.Ordinal);
        foreach (var resource in resources)
        {
            if (resource == null || string.IsNullOrEmpty(resource.Key))
                continue;

            valuesById[resource.Key] = resource.Value;
        }

        var orderedResources = new List<Bon>(ResourceOrder.Length);
        foreach (var resourceId in ResourceOrder)
        {
            orderedResources.Add(new Bon
            {
                Key = resourceId,
                Value = valuesById.TryGetValue(resourceId, out var value) ? value : 0
            });
        }

        wallet.Fill(orderedResources);
    }

    private void OnEnable()
    {
        Fill();
    }
}
