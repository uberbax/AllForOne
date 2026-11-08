using System.Collections.Generic;
using UnityEngine;

public class Slotter : MonoBehaviour
{
    public List<Transform> slots = new List<Transform>();

    private const float RefreshInterval = 0.2f;
    private const float FollowTime = 0.1f;
    private const float StrikeTime = 0.18f;
    private const float WindupTime = 0.065f;
    private const float WindupDistance = 0.14f;
    private const float WindupLift = 0.05f;
    private const float ReturnTime = 0.24f;
    private const float Penetration = 0.12f;
    private readonly List<SlotVisual> spawned = new();
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
            if (entry == null || entry.instance == null || entry.slot == null) continue;

            var visual = entry.instance.transform;
            var prefab = entry.prefab.transform;
            var position = entry.slot.TransformPoint(prefab.localPosition);
            var rotation = entry.slot.rotation * prefab.localRotation;
            visual.localScale = Vector3.Scale(entry.slot.lossyScale, prefab.localScale);
            if (entry.strike != null)
                UpdateStrike(entry, ref position, ref rotation);
            else
            {
                position = Vector3.Lerp(visual.position, position, blend);
                rotation = Quaternion.Slerp(visual.rotation, rotation, blend);
            }

            visual.SetPositionAndRotation(position, rotation);
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

            SetSlot(slotIndex, slots[slotIndex], prefab, id);
            slotIndex++;
        }

        for (int i = slotIndex; i < spawned.Count; i++)
            SetSlot(i, null, null);
    }

    public void Strike(string skillId, Transform target, Vector3 point)
    {
        if (!isActiveAndEnabled || target == null || string.IsNullOrEmpty(skillId)) return;

        RefreshSlots();
        foreach (var entry in spawned)
        {
            if (entry == null || entry.instance == null || entry.id != skillId) continue;

            var visual = entry.instance.transform;
            var pullback = (visual.position - point).normalized * WindupDistance;
            entry.strike = new StrikeMotion
            {
                target = target,
                localAim = target.InverseTransformPoint(point),
                aim = point,
                startPosition = visual.position,
                windupPosition = visual.position + pullback + Vector3.up * WindupLift,
                startRotation = visual.rotation
            };
            return;
        }
    }

    private static void UpdateStrike(SlotVisual entry, ref Vector3 position, ref Quaternion rotation)
    {
        var strike = entry.strike;
        bool advancing = strike.elapsed < StrikeTime;
        strike.elapsed += Time.deltaTime;
        if (advancing)
        {
            if (strike.target != null)
                strike.aim = strike.target.TransformPoint(strike.localAim);

            Vector3 direction = strike.aim - strike.startPosition;
            float distance = direction.magnitude;
            strike.hitPosition = strike.startPosition;
            strike.hitRotation = strike.startRotation;
            if (distance > 0.001f)
            {
                direction /= distance;
                var tip = Vector3.Scale(entry.localTip, entry.instance.transform.localScale);
                strike.hitRotation = Quaternion.FromToRotation(rotation * tip, direction) * rotation;
                strike.hitPosition += direction * Mathf.Max(0f, distance - tip.magnitude + Penetration);
            }
        }

        if (strike.elapsed < StrikeTime)
        {
            var windupRotation = Quaternion.Slerp(strike.startRotation, strike.hitRotation, 0.35f);
            if (strike.elapsed < WindupTime)
            {
                float progress = Mathf.SmoothStep(0f, 1f, strike.elapsed / WindupTime);
                position = Vector3.Lerp(strike.startPosition, strike.windupPosition, progress);
                rotation = Quaternion.Slerp(strike.startRotation, windupRotation, progress);
            }
            else
            {
                float progress = Mathf.SmoothStep(0f, 1f, (strike.elapsed - WindupTime) / (StrikeTime - WindupTime));
                position = Vector3.Lerp(strike.windupPosition, strike.hitPosition, progress);
                rotation = Quaternion.Slerp(windupRotation, strike.hitRotation, progress);
            }
        }
        else
        {
            float progress = Mathf.SmoothStep(0f, 1f, (strike.elapsed - StrikeTime) / ReturnTime);
            position = Vector3.Lerp(strike.hitPosition, position, progress);
            rotation = Quaternion.Slerp(strike.hitRotation, rotation, progress);
            if (strike.elapsed >= StrikeTime + ReturnTime)
                entry.strike = null;
        }
    }

    private static Vector3 GetLocalTip(GameObject prefab)
    {
        if (!prefab.TryGetComponent<MeshFilter>(out var filter) || filter.sharedMesh == null)
            return Vector3.forward * 0.2f;

        // These weapon meshes are pivoted at the grip and extend along their longest axis.
        Bounds bounds = filter.sharedMesh.bounds;
        Vector3 size = bounds.size;
        int axis = size.x > size.y ? 0 : 1;
        if (size.z > size[axis]) axis = 2;

        Vector3 tip = Vector3.zero;
        tip[axis] = Mathf.Abs(bounds.min[axis]) > Mathf.Abs(bounds.max[axis])
            ? bounds.min[axis]
            : bounds.max[axis];
        return tip;
    }

    private void SetSlot(int index, Transform slot, GameObject prefab, string id = null)
    {
        while (spawned.Count <= index)
            spawned.Add(null);

        var current = spawned[index];
        if (current != null && current.instance != null)
        {
            if (current.id == id && current.prefab == prefab && current.slot == slot) return;
            Destroy(current.instance);
        }

        spawned[index] = null;
        if (prefab == null) return;

        var position = slot.TransformPoint(prefab.transform.localPosition);
        var rotation = slot.rotation * prefab.transform.localRotation;
        var instance = Instantiate(prefab, position, rotation);
        if (instance.scene != gameObject.scene)
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(instance, gameObject.scene);
        instance.transform.localScale = Vector3.Scale(slot.lossyScale, prefab.transform.localScale);
        spawned[index] = new SlotVisual
        {
            id = id,
            slot = slot,
            prefab = prefab,
            instance = instance,
            localTip = GetLocalTip(prefab)
        };
    }

    private void OnDisable()
    {
        foreach (var entry in spawned)
        {
            if (entry == null || entry.instance == null) continue;
            entry.instance.SetActive(false);
            Destroy(entry.instance);
        }
        spawned.Clear();
    }

    private sealed class SlotVisual
    {
        public string id;
        public Transform slot;
        public GameObject prefab;
        public GameObject instance;
        public Vector3 localTip;
        public StrikeMotion strike;
    }

    private sealed class StrikeMotion
    {
        public Transform target;
        public Vector3 localAim;
        public Vector3 aim;
        public Vector3 startPosition;
        public Vector3 windupPosition;
        public Quaternion startRotation;
        public Vector3 hitPosition;
        public Quaternion hitRotation;
        public float elapsed;
    }
}
