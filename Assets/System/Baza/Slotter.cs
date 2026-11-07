using System.Collections.Generic;
using UnityEngine;

public class Slotter : MonoBehaviour
{
    public List<Transform> slots = new List<Transform>();

    private const float RefreshInterval = 0.2f;
    private const float FollowTime = 0.1f;
    private readonly List<(Transform slot, GameObject prefab, GameObject instance)> spawned = new();
    private ObjHolder holder;
    private float nextRefresh;

    private void OnEnable()
    {
        nextRefresh = 0f;
    }

    private void LateUpdate()
    {
        if (Time.unscaledTime >= nextRefresh)
            RefreshSlots();

        float blend = 1f - Mathf.Exp(-Time.deltaTime / FollowTime);
        foreach (var entry in spawned)
        {
            if (entry.instance == null || entry.slot == null) continue;

            var visual = entry.instance.transform;
            var prefab = entry.prefab.transform;
            var position = entry.slot.TransformPoint(prefab.localPosition);
            var rotation = entry.slot.rotation * prefab.localRotation;
            visual.SetPositionAndRotation(
                Vector3.Lerp(visual.position, position, blend),
                Quaternion.Slerp(visual.rotation, rotation, blend));
            visual.localScale = Vector3.Scale(entry.slot.lossyScale, prefab.localScale);
        }
    }

    public void RefreshSlots()
    {
        if (!isActiveAndEnabled) return;
        nextRefresh = Time.unscaledTime + RefreshInterval;

        if (holder == null) holder = GetComponentInParent<ObjHolder>();
        var mon = holder != null ? holder.obj : null;
        var resources = ResourceHolder.instance;
        if (mon == null || resources == null || resources.extraSkills == null) return;

        // The battle copy receives the equipped skills directly in actSkills.
        bool isBattleCopy = mon.RID == "second_main";
        var skills = isBattleCopy ? mon.actSkills : mon.inventory;
        int slotIndex = 0;
        foreach (var skill in skills)
        {
            while (slotIndex < slots.Count && slots[slotIndex] == null)
                SetSlot(slotIndex++, null, null);
            if (slotIndex == slots.Count) break;

            if (skill == null || (!isBattleCopy &&
                (skill.it != ItemType.projectile || skill.GetPar("used_slot") < 0))) continue;

            string id = skill.dbObj?.ID ?? skill.dbID;
            if (string.IsNullOrEmpty(id) || id == "basic_melee" ||
                !resources.extraSkills.TryGetValue(id, out var prefab) || prefab == null) continue;

            SetSlot(slotIndex, slots[slotIndex], prefab);
            slotIndex++;
        }

        for (int i = slotIndex; i < spawned.Count; i++)
            SetSlot(i, null, null);
    }

    private void SetSlot(int index, Transform slot, GameObject prefab)
    {
        while (spawned.Count <= index)
            spawned.Add(default);

        var current = spawned[index];
        if (current.instance != null)
        {
            if (current.prefab == prefab && current.slot == slot) return;
            Destroy(current.instance);
        }

        spawned[index] = default;
        if (prefab == null) return;

        var position = slot.TransformPoint(prefab.transform.localPosition);
        var rotation = slot.rotation * prefab.transform.localRotation;
        var instance = Instantiate(prefab, position, rotation);
        if (instance.scene != gameObject.scene)
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(instance, gameObject.scene);
        instance.transform.localScale = Vector3.Scale(slot.lossyScale, prefab.transform.localScale);
        spawned[index] = (slot, prefab, instance);
    }

    private void OnDisable()
    {
        foreach (var entry in spawned)
        {
            if (entry.instance == null) continue;
            entry.instance.SetActive(false);
            Destroy(entry.instance);
        }
        spawned.Clear();
    }
}
