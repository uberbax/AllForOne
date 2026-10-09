using System;
using System.Collections.Generic;
using UnityEngine;

namespace Animpic.FantasyCharacter
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Animpic Studio/Characters/Fantasy Character/Female Character Customizer")]
    public sealed class FemaleCharacterCustomizer : MonoBehaviour
    {
        [SerializeField] private FemaleCustomizationCatalog catalog;
        [SerializeField] private FemaleCustomizationSelection selection = new FemaleCustomizationSelection();
        public FemaleCustomizationCatalog Catalog { get { return catalog; } }
        public string LastValidationError { get; private set; }

        [Serializable]
        private sealed class SaveData
        {
            public int schemaVersion;
            public FemaleCustomizationSelection selection;
        }

        private void Awake()
        {
            if (catalog != null && !TryApply(selection, out var error))
                Debug.LogError("Female customization: " + error, this);
        }

        /// <summary>Assigns data only. Call TryApply to validate and update renderers.</summary>
        public void Configure(FemaleCustomizationCatalog value) { catalog = value; }

        public FemaleCustomizationSelection CaptureSelection()
        {
            return selection == null ? new FemaleCustomizationSelection() : selection.Copy();
        }

        public bool ValidateConfiguration(out string error)
        {
            bool valid = TryResolve(selection, out _, out _, out _, out error);
            LastValidationError = error;
            return valid;
        }

        /// <summary>Validates the complete input, catalogue, and hierarchy before changing anything.</summary>
        public bool TryApply(FemaleCustomizationSelection requested, out string error)
        {
            // Own a copy so a caller cannot alter a successfully applied selection later.
            var next = requested == null ? null : requested.Copy();
            if (!TryResolve(next, out var parts, out var bindings, out var desired, out error))
            {
                LastValidationError = error;
                return false;
            }

            var material = catalog.palettes[next.palette];
            // Prepare every material array before the first renderer mutation.
            var materials = new Dictionary<string, Material[]>(StringComparer.Ordinal);
            foreach (var part in parts)
            {
                var mesh = desired.TryGetValue(part.rendererName, out var visibleMesh) ? visibleMesh : part.mesh;
                var slots = new Material[mesh.subMeshCount];
                for (int i = 0; i < slots.Length; i++) slots[i] = material;
                materials.Add(part.rendererName, slots);
            }

            foreach (var part in parts)
                if (!desired.ContainsKey(part.rendererName)) bindings[part.rendererName].enabled = false;
            foreach (var part in parts)
            {
                var renderer = bindings[part.rendererName];
                renderer.sharedMesh = desired.TryGetValue(part.rendererName, out var mesh) ? mesh : part.mesh;
                renderer.sharedMaterials = materials[part.rendererName];
                renderer.enabled = desired.ContainsKey(part.rendererName);
            }
            selection = next;
            LastValidationError = null;
            return true;
        }

        public string SaveJson()
        {
            return JsonUtility.ToJson(new SaveData { schemaVersion = 1, selection = CaptureSelection() }, true);
        }

        public bool TryLoadJson(string json, out string error)
        {
            SaveData data;
            try
            {
                if (string.IsNullOrWhiteSpace(json) || !json.TrimStart().StartsWith("{", StringComparison.Ordinal) ||
                    !json.TrimEnd().EndsWith("}", StringComparison.Ordinal))
                    return Reject("Expected a JSON customization object.", out error);
                data = JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception exception)
            {
                return Reject("Invalid customization JSON: " + exception.Message, out error);
            }
            // JsonUtility can turn an explicitly null nested object into a default
            // instance. Require a real selection object in the envelope as well.
            bool selectionObject = System.Text.RegularExpressions.Regex.IsMatch(json,
                "\\\"selection\\\"\\s*:\\s*\\{");
            if (!selectionObject || data == null || data.schemaVersion != 1 || data.selection == null)
                return Reject("Unsupported or incomplete customization save; expected schemaVersion 1 and selection.", out error);
            return TryApply(data.selection, out error);
        }

        /// <summary>Reproducible local randomization that does not change UnityEngine.Random state.</summary>
        public bool Randomize(int seed, out string error)
        {
            var random = new System.Random(seed);
            var next = new FemaleCustomizationSelection
            {
                torso = random.Next(4), pants = random.Next(4), footwear = random.Next(7),
                leftArmMode = (FemaleArmMode)random.Next(2), rightArmMode = (FemaleArmMode)random.Next(2),
                leftForearmStyle = random.Next(-1, 4), rightForearmStyle = random.Next(-1, 4),
                leftGlove = random.Next(5), rightGlove = random.Next(5),
                hair = random.Next(4), brows = random.Next(4), palette = random.Next(6)
            };
            return TryApply(next, out error);
        }

        private bool Reject(string message, out string error)
        {
            LastValidationError = message;
            return FemaleCustomizationCatalog.Fail(message, out error);
        }

        private bool TryResolve(FemaleCustomizationSelection requested, out List<FemaleMeshPart> parts,
            out Dictionary<string, SkinnedMeshRenderer> bindings, out Dictionary<string, Mesh> desired, out string error)
        {
            parts = null;
            bindings = null;
            desired = null;
            if (requested == null) return FemaleCustomizationCatalog.Fail("Selection is missing.", out error);
            if (!requested.Validate(out error)) return false;
            if (catalog == null) return FemaleCustomizationCatalog.Fail("Customization catalogue is not configured.", out error);
            if (!catalog.TryGetParts(out parts, out error)) return false;

            bindings = new Dictionary<string, SkinnedMeshRenderer>(StringComparer.Ordinal);
            var expectedNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var part in parts) expectedNames.Add(part.rendererName);
            foreach (var renderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (!expectedNames.Contains(renderer.name)) continue;
                if (bindings.ContainsKey(renderer.name))
                    return FemaleCustomizationCatalog.Fail("Multiple renderers match binding " + renderer.name + ".", out error);
                bindings.Add(renderer.name, renderer);
            }
            foreach (var part in parts)
            {
                if (!bindings.TryGetValue(part.rendererName, out var renderer))
                    return FemaleCustomizationCatalog.Fail("Missing renderer: " + part.rendererName + ".", out error);
                // The installer activates imported mesh objects once. Runtime never activates bones or objects.
                for (var current = renderer.transform; current != transform && current != null; current = current.parent)
                    if (!current.gameObject.activeSelf)
                        return FemaleCustomizationCatalog.Fail("Inactive object in renderer path: " + part.rendererName + ". Run character setup first.", out error);
                if (!ValidSkin(renderer, part.mesh, part, out error)) return false;
            }

            foreach (var glove in catalog.gloves)
            {
                if (glove.handCoverage != FemaleHandCoverage.BareHandRequired) continue;
                if (!ValidSkin(bindings[catalog.leftHand.rendererName], glove.leftHandCompanion, catalog.leftHand, out error) ||
                    !ValidSkin(bindings[catalog.rightHand.rendererName], glove.rightHandCompanion, catalog.rightHand, out error)) return false;
            }

            desired = new Dictionary<string, Mesh>(StringComparer.Ordinal);
            var torso = catalog.torsos[requested.torso];
            Add(desired, catalog.head); Add(desired, torso.torso);
            bool highBoots = requested.footwear > 0 && catalog.footwear[requested.footwear - 1].high;
            Add(desired, highBoots ? catalog.pants[requested.pants].shortPants : catalog.pants[requested.pants].longPants);
            Add(desired, requested.footwear == 0 ? catalog.feet : catalog.footwear[requested.footwear - 1].part);
            if (requested.hair > 0) Add(desired, catalog.hair[requested.hair - 1]);
            if (requested.brows > 0) Add(desired, catalog.brows[requested.brows - 1]);
            ResolveArm(desired, requested, true); ResolveArm(desired, requested, false);

            error = null;
            return true;
        }

        private void ResolveArm(Dictionary<string, Mesh> desired, FemaleCustomizationSelection requested, bool left)
        {
            var torso = catalog.torsos[requested.torso];
            var arm = left ? torso.leftArm : torso.rightArm;
            int gloveIndex = left ? requested.leftGlove : requested.rightGlove;
            if (gloveIndex > 0)
            {
                var glove = catalog.gloves[gloveIndex - 1];
                Add(desired, arm.upper);
                Add(desired, left ? glove.left : glove.right);
                if (glove.handCoverage == FemaleHandCoverage.BareHandRequired)
                    desired.Add(left ? catalog.leftHand.rendererName : catalog.rightHand.rendererName,
                        left ? glove.leftHandCompanion : glove.rightHandCompanion);
                return;
            }
            var mode = left ? requested.leftArmMode : requested.rightArmMode;
            if (mode == FemaleArmMode.Full) { Add(desired, arm.full); return; }
            Add(desired, arm.upper);
            int style = left ? requested.leftForearmStyle : requested.rightForearmStyle;
            var lowerTorso = style < 0 ? torso : catalog.torsos[style];
            Add(desired, left ? lowerTorso.leftArm.forearm : lowerTorso.rightArm.forearm);
        }

        private static void Add(Dictionary<string, Mesh> desired, FemaleMeshPart part)
        {
            desired.Add(part.rendererName, part.mesh);
        }

        private bool ValidSkin(SkinnedMeshRenderer renderer, Mesh mesh, FemaleMeshPart definition, out string error)
        {
            var bones = renderer.bones;
            if (renderer.sharedMesh == null)
                return FemaleCustomizationCatalog.Fail("Missing original mesh on renderer " + renderer.name + ".", out error);
            if (bones.Length != definition.requiredBoneNames.Length)
                return FemaleCustomizationCatalog.Fail("Bone count does not match audited renderer " + renderer.name + ".", out error);
            if (!OwnsBone(renderer.rootBone) || renderer.rootBone.name != definition.requiredRootBoneName)
                return FemaleCustomizationCatalog.Fail("Root bone does not match this character's audited binding in " + renderer.name + ".", out error);
            for (int i = 0; i < bones.Length; i++)
                if (!OwnsBone(bones[i]) || bones[i].name != definition.requiredBoneNames[i])
                    return FemaleCustomizationCatalog.Fail("Bone ownership, name or order differs at index " + i + " in " + renderer.name + ".", out error);
#if UNITY_EDITOR
            // Non-readable FBX mesh arrays are available for editor auditing, but
            // must not be read by Player code or during the game/rendering loop.
            if (!Application.isPlaying)
            {
                var bindPoses = mesh.bindposes;
                var originalBindPoses = renderer.sharedMesh.bindposes;
                if (bindPoses.Length != bones.Length || originalBindPoses.Length != bindPoses.Length)
                    return FemaleCustomizationCatalog.Fail("Mesh skeleton does not match renderer " + renderer.name + ".", out error);
                for (int i = 0; i < bones.Length; i++)
                    for (int element = 0; element < 16; element++)
                    {
                        float candidate = bindPoses[i][element];
                        float original = originalBindPoses[i][element];
                        if (float.IsNaN(candidate) || float.IsInfinity(candidate) || float.IsNaN(original) || float.IsInfinity(original) ||
                            Mathf.Abs(candidate - original) > 0.00001f)
                            return FemaleCustomizationCatalog.Fail("Incompatible bind pose at bone " + i + " in " + renderer.name + ".", out error);
                    }
            }
#endif
            error = null;
            return true;
        }

        private bool OwnsBone(Transform bone)
        {
            return bone != null && (bone == transform || bone.IsChildOf(transform));
        }
    }
}