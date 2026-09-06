using System.Collections.Generic;
using UnityEngine;

public class GUIPlayerWallet : MonoBehaviour
{
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

    private void OnMinimusRefresh()
    {
        if (gameObject.activeInHierarchy)
            Fill();
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

        var resourceOrder = new[]
        {
            MainCycle_WhoHeroes.GoldResourceId,
            MainCycle_WhoHeroes.WoodResourceId,
            MainCycle_WhoHeroes.StoneResourceId,
            "gem",
            "ore"
        };
        var orderedResources = new List<Bon>(resourceOrder.Length);
        for (var index = 0; index < resourceOrder.Length; index++)
        {
            var resourceId = resourceOrder[index];
            orderedResources.Add(new Bon
            {
                Key = resourceId,
                Value = index < 3 && valuesById.TryGetValue(resourceId, out var value) ? value : 0
            });
        }

        wallet.Fill(orderedResources);
    }

    private void OnEnable()
    {
        UIfiller.acts += OnMinimusRefresh;
        Fill();
    }

    private void OnDisable()
    {
        UIfiller.acts -= OnMinimusRefresh;
    }
}
