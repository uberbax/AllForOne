using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PartyController : MonoBehaviour
{
    public List<RObj> party = new List<RObj>();
    [SerializeField] private Vector3 spawnOffset = new Vector3(1f, 0f, 0f);

    private sealed class Companion
    {
        public RObj unit;
        public int positionIndex;
    }

    // Track inventory instances, so identical monster IDs can have separate companions.
    private readonly Dictionary<RObj, Companion> companions = new Dictionary<RObj, Companion>();
    private readonly HashSet<RObj> equipped = new HashSet<RObj>();
    private readonly HashSet<int> occupiedPositions = new HashSet<int>();
    private readonly List<RObj> removedEquipment = new List<RObj>();
    private RObj player;
    private MainStates state;

    private void Update() => RefreshParty();

    /// <summary>Synchronize world companions with all equipped monsters.</summary>
    public void RefreshParty()
    {
        if (!isActiveAndEnabled) return;
        var currentState = MainStates.instance;
        if (currentState == null || !currentState.HasMain())
        {
            ClearCompanions();
            return;
        }

        var currentPlayer = currentState.mainPlayer;
        if (state != currentState || player != currentPlayer)
        {
            ClearCompanions();
            state = currentState;
            player = currentPlayer;
        }
        if (currentPlayer == null || currentPlayer.main == null) return;

        equipped.Clear();
        foreach (var item in currentPlayer.inventory)
            if (item != null && item.it == ItemType.monster && item.GetPar("used_slot") >= 0)
                equipped.Add(item);

        removedEquipment.Clear();
        occupiedPositions.Clear();
        foreach (var entry in companions)
        {
            if (!equipped.Contains(entry.Key))
            {
                RemoveUnit(entry.Value.unit);
                removedEquipment.Add(entry.Key);
                continue;
            }
            occupiedPositions.Add(entry.Value.positionIndex);
            var unit = entry.Value.unit;
            if (unit != null && unit.main == null)
            {
                RemoveUnit(unit);
                // Keep the equipment entry until unequipped, preventing automatic resurrection.
                entry.Value.unit = null;
            }
            else if (unit != null && !party.Contains(unit)) party.Add(unit);
        }
        foreach (var item in removedEquipment) companions.Remove(item);

        if (WaveSpawner.instance == null || DatabaseAll.instance == null || ResourceHolder.instance == null) return;
        // Iterate inventory for stable spawn ordering without allocating a filtered list each frame.
        foreach (var item in currentPlayer.inventory)
        {
            if (item == null || !equipped.Contains(item) || companions.ContainsKey(item)) continue;
            int index = 0;
            while (occupiedPositions.Contains(index)) index++;
            Vector3 position = currentPlayer.main.transform.position + spawnOffset * (index + 1);
            var spawned = WaveSpawner.instance.DoSpawnAny(
                new List<Bon> { new Bon { Key = item.dbID, Value = 1, Val3 = Mathf.Max(1, (int)item.GetPar("level")) } },
                "player", null, null, false, position, position, overridesViz: MainStates.overridesViz);
            if (spawned.Count == 0) continue;
            var companion = spawned[0];
            companion.owner = currentPlayer;
            companions.Add(item, new Companion { unit = companion, positionIndex = index });
            occupiedPositions.Add(index);
            party.Add(companion);
            //
            companion.localPos = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f), 0);
            companion.AddViz("follow2");
        }
    }

    private void RemoveUnit(RObj unit)
    {
        if (unit == null) return;
        party.Remove(unit);
        if (state != null)
        {
            if (state.all.TryGetValue(unit.RID, out var registered) && registered == unit)
                state.all.Remove(unit.RID);
            if (state.allMons.TryGetValue(unit.RID, out registered) && registered == unit)
                state.allMons.Remove(unit.RID);
        }
        if (unit.main != null) Destroy(unit.main);
    }

    private void ClearCompanions()
    {
        foreach (var entry in companions) RemoveUnit(entry.Value.unit);
        companions.Clear();
        equipped.Clear();
        occupiedPositions.Clear();
        removedEquipment.Clear();
        player = null;
        state = null;
    }

    private void OnDisable() => ClearCompanions();

    public static PartyController instance;
    private void Awake()
    {
        instance = this;
    }
}
