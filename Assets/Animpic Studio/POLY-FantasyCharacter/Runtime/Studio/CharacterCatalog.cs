using System;
using System.Collections.Generic;
using UnityEngine;

namespace Animpic.CharacterStudio
{
    [Serializable] public sealed class PartDefinition
    {
        public string id, label;
        public Mesh mesh;
        public string[] requiredBoneNames;
        public string requiredRootBoneName;
    }
    [Serializable] public class CharacterOption { public string id, label; public string[] parts = new string[0]; }
    [Serializable] public sealed class ArmSet { public string[] full, upper, forearm; }
    [Serializable] public sealed class TorsoOption : CharacterOption { public ArmSet leftArm, rightArm; }
    [Serializable] public sealed class PantsOption { public string id, label; public string[] longParts, shortParts; }
    [Serializable] public sealed class FootwearOption : CharacterOption { public bool high; }
    [Serializable] public sealed class GloveOption
    {
        public string id, label;
        public string[] leftParts, rightParts;
        public string leftCompanionPartId, rightCompanionPartId;
        public Mesh leftCompanionMesh, rightCompanionMesh;
    }
    [Serializable] public sealed class ExtraOption : CharacterOption { public bool hideHair, hideBeard; }
    [Serializable] public sealed class ExtraSlot { public string id, label; public ExtraOption[] options = new ExtraOption[0]; }

    [CreateAssetMenu(menuName = "Animpic Studio/Characters/Fantasy Character/Catalog")]
    public sealed class CharacterCatalog : ScriptableObject
    {
        public string id, label;
        public PartDefinition[] parts = new PartDefinition[0];
        public Material[] palettes = new Material[6];
        public string[] tintProperties = { "_Color" };
        public string[] headParts, bareFeetParts;
        public TorsoOption[] torsos = new TorsoOption[0];
        public PantsOption[] pants = new PantsOption[0];
        public FootwearOption[] footwear = new FootwearOption[0];
        public GloveOption[] gloves = new GloveOption[0];
        public CharacterOption[] hair = new CharacterOption[0], brows = new CharacterOption[0], beards = new CharacterOption[0];
        public ExtraSlot[] extras = new ExtraSlot[0];

        public bool Validate(out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(id) || parts == null || parts.Length == 0 || palettes == null || palettes.Length != 6)
                return Fail("Catalog requires an ID, parts, and six palette materials.", out error);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var p in parts)
            {
                if (p == null || string.IsNullOrWhiteSpace(p.id) || !ids.Add(p.id) || !Usable(p.mesh))
                    return Fail("Missing or duplicate part ID, or missing part mesh.", out error);
                if (p.requiredBoneNames == null || p.requiredBoneNames.Length == 0 || string.IsNullOrWhiteSpace(p.requiredRootBoneName))
                    return Fail("Missing audited rig metadata: " + p.id, out error);
                foreach (var bone in p.requiredBoneNames) if (string.IsNullOrWhiteSpace(bone)) return Fail("Missing audited bone: " + p.id, out error);
            }
            if (tintProperties == null || tintProperties.Length == 0) return Fail("A tint color property is required.", out error);
            foreach (var name in tintProperties) if (string.IsNullOrWhiteSpace(name)) return Fail("Empty tint property name.", out error);
            foreach (var material in palettes)
            {
                if (material == null || material.shader == null) return Fail("Missing palette material or shader.", out error);
                bool supportsTint = false;
                foreach (var name in tintProperties) supportsTint |= IsColorProperty(material, name);
                if (!supportsTint) return Fail("Palette has no configured Color property: " + material.name, out error);
            }
            if (!Refs(headParts, ids, false) || !Refs(bareFeetParts, ids, false)) return Fail("Invalid head or bare-foot parts.", out error);
            if (torsos == null || torsos.Length == 0 || pants == null || pants.Length == 0 || footwear == null || gloves == null || extras == null)
                return Fail("Catalog option arrays are missing.", out error);
            var optionIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var t in torsos)
                if (t == null || !OptionId(t.id, optionIds) || !Refs(t.parts, ids, false) || !Arm(t.leftArm, ids) || !Arm(t.rightArm, ids))
                    return Fail("Invalid torso or arm definitions.", out error);
            optionIds.Clear();
            foreach (var p in pants)
                if (p == null || !OptionId(p.id, optionIds) || !Refs(p.longParts, ids, false) || !Refs(p.shortParts, ids, false))
                    return Fail("Invalid pants definition.", out error);
            if (!Options(footwear, ids) || !Options(hair, ids) || !Options(brows, ids) || !Options(beards, ids))
                return Fail("Invalid footwear, hair, brows, or beard options.", out error);
            optionIds.Clear();
            foreach (var glove in gloves)
                if (glove == null || !OptionId(glove.id, optionIds) || !Refs(glove.leftParts, ids, false) || !Refs(glove.rightParts, ids, false) ||
                    !Companion(glove.leftCompanionPartId, glove.leftCompanionMesh, ids) || !Companion(glove.rightCompanionPartId, glove.rightCompanionMesh, ids))
                    return Fail("Invalid glove or companion definition.", out error);
            optionIds.Clear();
            foreach (var slot in extras)
                if (slot == null || !OptionId(slot.id, optionIds) || !Options(slot.options, ids)) return Fail("Invalid extra slot.", out error);
            return true;
        }
        internal static bool IsColorProperty(Material material, string name)
        {
            int index = material.shader.FindPropertyIndex(name);
            return index >= 0 && material.shader.GetPropertyType(index) == UnityEngine.Rendering.ShaderPropertyType.Color;
        }
        private static bool Companion(string id, Mesh mesh, HashSet<string> ids)
        {
            return string.IsNullOrEmpty(id) ? mesh == null : ids.Contains(id) && Usable(mesh);
        }
        private static bool Arm(ArmSet arm, HashSet<string> ids)
        {
            return arm != null && Refs(arm.full, ids, false) && Refs(arm.upper, ids, false) && Refs(arm.forearm, ids, false);
        }
        private static bool Options<T>(T[] options, HashSet<string> ids) where T : CharacterOption
        {
            if (options == null) return false;
            var optionIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var option in options) if (option == null || !OptionId(option.id, optionIds) || !Refs(option.parts, ids, false)) return false;
            return true;
        }
        private static bool OptionId(string id, HashSet<string> ids) { return !string.IsNullOrWhiteSpace(id) && ids.Add(id); }
        private static bool Refs(string[] refs, HashSet<string> ids, bool allowEmpty)
        {
            if (refs == null || (!allowEmpty && refs.Length == 0)) return false;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in refs) if (id == null || !ids.Contains(id) || !seen.Add(id)) return false;
            return true;
        }
        internal static bool Usable(Mesh mesh) { return mesh != null && mesh.vertexCount > 0 && mesh.subMeshCount > 0; }
        internal static bool Fail(string text, out string error) { error = text; return false; }
    }
}