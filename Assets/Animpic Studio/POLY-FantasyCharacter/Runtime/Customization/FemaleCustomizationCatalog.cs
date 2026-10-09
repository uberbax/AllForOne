using System;
using System.Collections.Generic;
using UnityEngine;

namespace Animpic.FantasyCharacter
{
    [Serializable]
    public sealed class FemaleMeshPart
    {
        public string rendererName;
        public Mesh mesh;
        // Captured from the source renderer by Editor setup; no mesh CPU data is
        // required in a Player merely to validate a renderer's skeleton binding.
        public string[] requiredBoneNames;
        public string requiredRootBoneName;
    }

    [Serializable]
    public sealed class FemaleArmSet
    {
        public FemaleMeshPart full;
        public FemaleMeshPart upper;
        public FemaleMeshPart forearm;
    }

    [Serializable]
    public sealed class FemaleTorsoOption
    {
        public FemaleMeshPart torso;
        public FemaleArmSet leftArm;
        public FemaleArmSet rightArm;
    }

    [Serializable]
    public sealed class FemalePantsOption
    {
        public FemaleMeshPart longPants;
        public FemaleMeshPart shortPants;
    }

    [Serializable]
    public sealed class FemaleFootwearOption
    {
        public FemaleMeshPart part;
        public bool high;
    }

    [Serializable]
    public sealed class FemaleGloveOption
    {
        public FemaleMeshPart left;
        public FemaleMeshPart right;
        public FemaleHandCoverage handCoverage;
        // Fingerless gloves use explicit companion meshes on the bare-hand renderers.
        // A missing companion is an error, never a fallback to an overlapping full hand.
        public Mesh leftHandCompanion;
        public Mesh rightHandCompanion;
    }

    [CreateAssetMenu(menuName = "Animpic Studio/Characters/Fantasy Character/Female Customization Catalog")]
    public sealed class FemaleCustomizationCatalog : ScriptableObject
    {
        public FemaleMeshPart head;
        public FemaleMeshPart feet;
        public FemaleMeshPart leftHand;
        public FemaleMeshPart rightHand;
        public FemaleTorsoOption[] torsos = new FemaleTorsoOption[4];
        public FemalePantsOption[] pants = new FemalePantsOption[4];
        public FemaleFootwearOption[] footwear = new FemaleFootwearOption[6];
        public FemaleGloveOption[] gloves = new FemaleGloveOption[4];
        public FemaleMeshPart[] hair = new FemaleMeshPart[3];
        public FemaleMeshPart[] brows = new FemaleMeshPart[3];
        public Material[] palettes = new Material[6];

        /// <summary>Returns every owned renderer binding after checking the complete catalogue.</summary>
        public bool TryGetParts(out List<FemaleMeshPart> parts, out string error)
        {
            parts = new List<FemaleMeshPart>(60);
            error = null;
            if (!Length(torsos, 4) || !Length(pants, 4) || !Length(footwear, 6) ||
                !Length(gloves, 4) || !Length(hair, 3) || !Length(brows, 3) || !Length(palettes, 6))
                return Fail("Catalogue requires 4 torsos, 4 pants, 6 footwear, 4 gloves, 3 hair, 3 brows and 6 palettes.", out error);
            parts.Add(head); parts.Add(feet); parts.Add(leftHand); parts.Add(rightHand);
            for (int i = 0; i < 4; i++)
            {
                if (torsos[i] == null || torsos[i].leftArm == null || torsos[i].rightArm == null || pants[i] == null || gloves[i] == null)
                    return Fail("Missing torso, arm set, pants or glove option at index " + i + ".", out error);
                parts.Add(torsos[i].torso);
                AddArm(parts, torsos[i].leftArm); AddArm(parts, torsos[i].rightArm);
                parts.Add(pants[i].longPants); parts.Add(pants[i].shortPants);
                parts.Add(gloves[i].left); parts.Add(gloves[i].right);
                var glove = gloves[i];
                if (glove.handCoverage != FemaleHandCoverage.ReplacesHand && glove.handCoverage != FemaleHandCoverage.BareHandRequired)
                    return Fail("Invalid glove hand coverage at index " + i + ".", out error);
                if (glove.handCoverage == FemaleHandCoverage.BareHandRequired && (!UsableMesh(glove.leftHandCompanion) || !UsableMesh(glove.rightHandCompanion)))
                    return Fail("Fingerless glove " + i + " requires both explicit hand companion meshes.", out error);
                if (glove.handCoverage == FemaleHandCoverage.ReplacesHand && (glove.leftHandCompanion != null || glove.rightHandCompanion != null))
                    return Fail("Glove " + i + " replaces the hand but has unused companion meshes.", out error);
            }
            for (int i = 0; i < footwear.Length; i++)
            {
                if (footwear[i] == null) return Fail("Missing footwear option " + i + ".", out error);
                parts.Add(footwear[i].part);
            }
            parts.AddRange(hair); parts.AddRange(brows);
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var part in parts)
            {
                if (part == null || string.IsNullOrWhiteSpace(part.rendererName) || !UsableMesh(part.mesh))
                    return Fail("Every part requires a renderer name and a nonempty mesh.", out error);
                if (part.requiredBoneNames == null || part.requiredBoneNames.Length == 0 || string.IsNullOrWhiteSpace(part.requiredRootBoneName))
                    return Fail("Missing audited skeleton metadata for " + part.rendererName + ". Run character setup.", out error);
                foreach (var boneName in part.requiredBoneNames)
                    if (string.IsNullOrWhiteSpace(boneName)) return Fail("Missing audited bone name for " + part.rendererName + ".", out error);
                if (!names.Add(part.rendererName))
                    return Fail("Duplicate catalogue renderer binding: " + part.rendererName + ".", out error);
            }
            for (int i = 0; i < palettes.Length; i++)
                if (palettes[i] == null) return Fail("Missing palette material " + i + ".", out error);
            return true;
        }

        private static void AddArm(List<FemaleMeshPart> parts, FemaleArmSet arm)
        {
            parts.Add(arm.full); parts.Add(arm.upper); parts.Add(arm.forearm);
        }

        private static bool Length(Array value, int expected) { return value != null && value.Length == expected; }
        internal static bool UsableMesh(Mesh mesh) { return mesh != null && mesh.vertexCount > 0 && mesh.subMeshCount > 0; }
        internal static bool Fail(string message, out string error) { error = message; return false; }
    }
}
