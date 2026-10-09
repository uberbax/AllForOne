using System;
using System.Collections.Generic;
using UnityEngine;

namespace Animpic.CharacterStudio
{
    [ExecuteAlways, DisallowMultipleComponent, AddComponentMenu("Animpic Studio/Characters/Fantasy Character/Customizer")]
    public sealed class CharacterCustomizer : MonoBehaviour
    {
        [SerializeField] private CharacterCatalog catalog;
        [SerializeField] private Appearance appearance = new Appearance();
        public CharacterCatalog Catalog { get { return catalog; } }
        public string LastValidationError { get; private set; }
        public event Action Changed;
        [Serializable] private sealed class Save { public int schemaVersion = 1; public Appearance appearance; }
        private sealed class Change
        {
            public SkinnedMeshRenderer renderer; public Mesh mesh; public bool visible;
            public Material[] materials; public MaterialPropertyBlock block;
        }
        // Property blocks are not serialized. Restore them once when a scene or
        // prefab-stage instance loads, reloads, or becomes enabled in either mode.
        private void OnEnable()
        {
            if (catalog != null && gameObject.scene.IsValid() && !TryApply(Capture(), out var error)) Debug.LogError(error, this);
        }
        public void Configure(CharacterCatalog value) { catalog = value; }
        public Appearance Capture()
        {
            var copy = appearance == null ? new Appearance() : appearance.Copy();
            if (string.IsNullOrEmpty(copy.catalogId) && catalog != null) copy.catalogId = catalog.id;
            return copy;
        }
        public string SaveJson() { return JsonUtility.ToJson(new Save { appearance = Capture() }, true); }
        public bool TryLoadJson(string json, out string error)
        {
            Appearance loaded;
            try { loaded = AppearanceJson.Read(json); }
            catch (FormatException e) { return Reject(e.Message, out error); }
            return TryApply(loaded, out error);
        }
        public bool ValidateConfiguration(out string error)
        {
            bool valid = Resolve(Capture(), out _, out _, out error); LastValidationError = error; return valid;
        }
        public bool TryApply(Appearance requested, out string error)
        {
            var next = requested == null ? null : requested.Copy();
            if (!Resolve(next, out var bindings, out var desired, out error)) { LastValidationError = error; return false; }
            var paints = new Dictionary<string, PartPaint>(StringComparer.Ordinal);
            foreach (var p in next.paints) paints.Add(p.partId, p);
            var changes = new List<Change>(catalog.parts.Length);
            foreach (var part in catalog.parts)
            {
                var renderer = bindings[part.id]; bool visible = desired.TryGetValue(part.id, out var mesh);
                if (!visible) mesh = part.mesh;
                paints.TryGetValue(part.id, out var paint);
                var material = catalog.palettes[paint == null || paint.palette < 0 ? next.basePalette : paint.palette];
                var materials = new Material[mesh.subMeshCount]; for (int i = 0; i < materials.Length; i++) materials[i] = material;
                var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block);
                foreach (var name in catalog.tintProperties)
                    if (CharacterCatalog.IsColorProperty(material, name)) block.SetColor(name, paint == null ? Color.white : paint.tint);
                changes.Add(new Change { renderer = renderer, mesh = mesh, visible = visible, materials = materials, block = block });
            }
            // All validation and allocation happen above. Never activate an object,
            // move/rebind a bone, or alter a Material asset while applying appearance.
            foreach (var change in changes) if (!change.visible) change.renderer.enabled = false;
            foreach (var change in changes)
            {
                change.renderer.sharedMesh = change.mesh; change.renderer.sharedMaterials = change.materials;
                change.renderer.SetPropertyBlock(change.block); change.renderer.enabled = change.visible;
            }
            appearance = next; LastValidationError = null;
            var listeners = Changed;
            if (listeners != null) foreach (Action listener in listeners.GetInvocationList())
                try { listener(); } catch (Exception e) { Debug.LogException(e, this); }
            return true;
        }
        public bool Randomize(int seed, out string error)
        {
            if (catalog == null) return Reject("Missing catalog.", out error);
            if (!catalog.Validate(out error)) return Reject(error, out error);
            var next = Capture(); var random = new System.Random(seed);
            next.torso = random.Next(catalog.torsos.Length); next.pants = random.Next(catalog.pants.Length); next.footwear = random.Next(catalog.footwear.Length + 1);
            next.leftArmMode = (ArmMode)random.Next(2); next.rightArmMode = (ArmMode)random.Next(2);
            next.leftForearmStyle = random.Next(-1, catalog.torsos.Length); next.rightForearmStyle = random.Next(-1, catalog.torsos.Length);
            next.leftGlove = random.Next(catalog.gloves.Length + 1); next.rightGlove = random.Next(catalog.gloves.Length + 1);
            next.hair = random.Next(catalog.hair.Length + 1); next.brows = random.Next(catalog.brows.Length + 1); next.beard = random.Next(catalog.beards.Length + 1);
            next.basePalette = random.Next(6); next.extras = new SlotChoice[catalog.extras.Length];
            for (int i = 0; i < next.extras.Length; i++) next.extras[i] = new SlotChoice { id = catalog.extras[i].id, option = random.Next(catalog.extras[i].options.Length + 1) };
            return TryApply(next, out error);
        }
        public VisiblePart[] GetVisibleParts()
        {
            var visible = new List<VisiblePart>(); if (catalog == null || catalog.parts == null) return visible.ToArray();
            var renderers = new Dictionary<string, SkinnedMeshRenderer>(StringComparer.Ordinal);
            foreach (var renderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (!renderers.ContainsKey(renderer.name)) renderers.Add(renderer.name, renderer);
            foreach (var p in catalog.parts)
                if (p != null && p.id != null && renderers.TryGetValue(p.id, out var renderer) && renderer.enabled)
                    visible.Add(new VisiblePart { id = p.id, label = string.IsNullOrEmpty(p.label) ? p.id : p.label, renderer = renderer });
            return visible.ToArray();
        }
        private bool Reject(string message, out string error) { LastValidationError = message; return CharacterCatalog.Fail(message, out error); }
        private bool Resolve(Appearance a, out Dictionary<string, SkinnedMeshRenderer> bindings, out Dictionary<string, Mesh> desired, out string error)
        {
            bindings = null; desired = null; error = null;
            if (catalog == null || a == null) return CharacterCatalog.Fail("Missing catalog or appearance.", out error);
            if (!catalog.Validate(out error) || !a.Validate(catalog, out error)) return false;
            var definitions = new Dictionary<string, PartDefinition>(StringComparer.Ordinal);
            foreach (var p in catalog.parts) definitions.Add(p.id, p);
            bindings = new Dictionary<string, SkinnedMeshRenderer>(StringComparer.Ordinal);
            foreach (var renderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (!definitions.ContainsKey(renderer.name)) continue;
                if (bindings.ContainsKey(renderer.name)) return CharacterCatalog.Fail("Duplicate renderer binding: " + renderer.name, out error);
                bindings.Add(renderer.name, renderer);
            }
            foreach (var part in catalog.parts)
            {
                if (!bindings.TryGetValue(part.id, out var renderer)) return CharacterCatalog.Fail("Missing renderer: " + part.id, out error);
                for (var t = renderer.transform; t != transform && t != null; t = t.parent)
                    if (!t.gameObject.activeSelf) return CharacterCatalog.Fail("Inactive mesh path: " + part.id + ". Run character setup first.", out error);
                if (!Skin(renderer, part.mesh, part, out error)) return false;
            }
            foreach (var g in catalog.gloves)
            {
                if (!string.IsNullOrEmpty(g.leftCompanionPartId) && !Skin(bindings[g.leftCompanionPartId], g.leftCompanionMesh, definitions[g.leftCompanionPartId], out error)) return false;
                if (!string.IsNullOrEmpty(g.rightCompanionPartId) && !Skin(bindings[g.rightCompanionPartId], g.rightCompanionMesh, definitions[g.rightCompanionPartId], out error)) return false;
            }
            desired = new Dictionary<string, Mesh>(StringComparer.Ordinal);
            var selectedExtras = new List<ExtraOption>(); bool hideHair = false, hideBeard = false;
            foreach (var choice in a.extras) if (choice.option > 0)
                foreach (var slot in catalog.extras) if (slot.id == choice.id)
                { var extra = slot.options[choice.option - 1]; selectedExtras.Add(extra); hideHair |= extra.hideHair; hideBeard |= extra.hideBeard; }
            var torso = catalog.torsos[a.torso];
            bool high = a.footwear > 0 && catalog.footwear[a.footwear - 1].high;
            if (!Add(catalog.headParts, definitions, desired, out error) || !Add(torso.parts, definitions, desired, out error) ||
                !Add(high ? catalog.pants[a.pants].shortParts : catalog.pants[a.pants].longParts, definitions, desired, out error) ||
                !Add(a.footwear == 0 ? catalog.bareFeetParts : catalog.footwear[a.footwear - 1].parts, definitions, desired, out error) ||
                !Arm(a, true, definitions, desired, out error) || !Arm(a, false, definitions, desired, out error)) return false;
            if (a.hair > 0 && !hideHair && !Add(catalog.hair[a.hair - 1].parts, definitions, desired, out error)) return false;
            if (a.brows > 0 && !Add(catalog.brows[a.brows - 1].parts, definitions, desired, out error)) return false;
            if (a.beard > 0 && !hideBeard && !Add(catalog.beards[a.beard - 1].parts, definitions, desired, out error)) return false;
            foreach (var extra in selectedExtras) if (!Add(extra.parts, definitions, desired, out error)) return false;
            return true;
        }
        private bool Arm(Appearance a, bool left, Dictionary<string, PartDefinition> defs, Dictionary<string, Mesh> desired, out string error)
        {
            var torso = catalog.torsos[a.torso]; var arm = left ? torso.leftArm : torso.rightArm;
            int gloveIndex = left ? a.leftGlove : a.rightGlove;
            if (gloveIndex > 0)
            {
                var glove = catalog.gloves[gloveIndex - 1];
                if (!Add(arm.upper, defs, desired, out error) || !Add(left ? glove.leftParts : glove.rightParts, defs, desired, out error)) return false;
                string companionId = left ? glove.leftCompanionPartId : glove.rightCompanionPartId;
                if (!string.IsNullOrEmpty(companionId))
                {
                    if (desired.ContainsKey(companionId)) return CharacterCatalog.Fail("Overlapping companion binding: " + companionId, out error);
                    desired.Add(companionId, left ? glove.leftCompanionMesh : glove.rightCompanionMesh);
                }
                return true;
            }
            if ((left ? a.leftArmMode : a.rightArmMode) == ArmMode.Full) return Add(arm.full, defs, desired, out error);
            int style = left ? a.leftForearmStyle : a.rightForearmStyle; var lower = style < 0 ? torso : catalog.torsos[style];
            return Add(arm.upper, defs, desired, out error) && Add(left ? lower.leftArm.forearm : lower.rightArm.forearm, defs, desired, out error);
        }
        private static bool Add(string[] ids, Dictionary<string, PartDefinition> defs, Dictionary<string, Mesh> desired, out string error)
        {
            foreach (var id in ids)
            { if (desired.ContainsKey(id)) return CharacterCatalog.Fail("Two selected options occupy renderer " + id, out error); desired.Add(id, defs[id].mesh); }
            error = null; return true;
        }
        private bool Skin(SkinnedMeshRenderer renderer, Mesh mesh, PartDefinition part, out string error)
        {
            var bones = renderer.bones;
            if (renderer.sharedMesh == null || bones.Length != part.requiredBoneNames.Length || !Owns(renderer.rootBone) || renderer.rootBone.name != part.requiredRootBoneName)
                return CharacterCatalog.Fail("Invalid audited rig binding: " + part.id, out error);
            for (int i = 0; i < bones.Length; i++) if (!Owns(bones[i]) || bones[i].name != part.requiredBoneNames[i])
                return CharacterCatalog.Fail("Invalid bone ownership, name or order: " + part.id, out error);
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var poses = mesh.bindposes; var baseline = renderer.sharedMesh.bindposes;
                if (poses.Length != bones.Length || baseline.Length != poses.Length) return CharacterCatalog.Fail("Bind-pose count mismatch: " + part.id, out error);
                for (int i = 0; i < poses.Length; i++) for (int element = 0; element < 16; element++)
                {
                    float x = poses[i][element], y = baseline[i][element];
                    if (float.IsNaN(x) || float.IsInfinity(x) || float.IsNaN(y) || float.IsInfinity(y) || Mathf.Abs(x - y) > 0.00001f)
                        return CharacterCatalog.Fail("Incompatible bind pose: " + part.id, out error);
                }
            }
#endif
            error = null; return true;
        }
        private bool Owns(Transform bone) { return bone != null && (bone == transform || bone.IsChildOf(transform)); }
    }
}