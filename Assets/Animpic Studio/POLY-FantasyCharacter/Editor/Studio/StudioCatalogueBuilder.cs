using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Animpic.FantasyCharacter.Editor;
using Object = UnityEngine.Object;

namespace Animpic.CharacterStudio.Editor
{
    public static class StudioCatalogueBuilder
    {
        public const string AssetRoot = "Assets/Animpic Studio/POLY-FantasyCharacter";
        public const string StudioRoot = "Assets/Animpic Studio/POLY-FantasyCharacter/Runtime/Studio";
        public const string FemalePrefab = "Assets/Animpic Studio/POLY-FantasyCharacter/Prefabs/POLY_FantasyFemale_Studio.prefab";
        public const string MalePrefab = "Assets/Animpic Studio/POLY-FantasyCharacter/Prefabs/POLY_FantasyMale_Studio.prefab";
        private const string GeneratedForearm = "Generated_D_Forearm_R";

        public static CharacterCatalog Build(bool male)
        {
            Ensure("Assets/Animpic Studio/POLY-FantasyCharacter/Runtime/Studio/Data"); Ensure("Assets/Animpic Studio/POLY-FantasyCharacter/Materials/Studio"); Ensure("Assets/Animpic Studio/POLY-FantasyCharacter/Animations/Studio");
            string sourcePath = "Assets/Animpic Studio/POLY-FantasyCharacter/Prefabs/" + (male ? "POLY_FantasyMale_SK" : "POLY_FantasyFemale_Unity") + ".prefab";
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (!source) throw new InvalidOperationException("Missing source: " + sourcePath);
            var renderers = source.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length != (male ? 65 : 60)) throw new InvalidOperationException("Source mesh inventory has changed. Re-audit before rebuilding.");
            var lookup = renderers.ToDictionary(r => r.name);
            string catalogPath = "Assets/Animpic Studio/POLY-FantasyCharacter/Runtime/Studio/Data/" + (male ? "Male" : "Female") + "CharacterCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<CharacterCatalog>(catalogPath);
            bool isNew = !catalog;
            if (!catalog) catalog = ScriptableObject.CreateInstance<CharacterCatalog>();
            catalog.id = male ? "poly-fantasy-male" : "poly-fantasy-female";
            catalog.label = male ? "Male" : "Female";
            catalog.palettes = Palettes(); catalog.tintProperties = new[] { "_Color" };
            var definitions = renderers.Select(r => Definition(r, r.name, r.name, r.sharedMesh)).ToDictionary(p => p.id);
            Action<string, string> label = (id, text) => definitions[id].label = text;
            if (male) PopulateMale(catalog, lookup, definitions, label);
            else PopulateFemale(catalog, definitions, label);
            catalog.parts = definitions.Values.OrderBy(p => p.id, StringComparer.Ordinal).ToArray();
            catalog.extras = new ExtraSlot[0];
            string error;
            if (!catalog.Validate(out error)) throw new InvalidOperationException(error);
            if (isNew) AssetDatabase.CreateAsset(catalog, catalogPath);
            EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssetIfDirty(catalog);

            var scratch = PrefabUtility.LoadPrefabContents(sourcePath);
            try
            {
                if (male) AddGeneratedForearm(scratch, definitions[GeneratedForearm].mesh);
                foreach (var renderer in scratch.GetComponentsInChildren<SkinnedMeshRenderer>(true)) renderer.gameObject.SetActive(true);
                var customizer = scratch.GetComponent<CharacterCustomizer>();
                if (!customizer) customizer = scratch.AddComponent<CharacterCustomizer>();
                customizer.Configure(catalog);
                if (!customizer.TryApply(DefaultAppearance(catalog), out error)) throw new InvalidOperationException(error);
                var animator = scratch.GetComponent<Animator>();
                if (!animator) animator = scratch.AddComponent<Animator>();
                animator.runtimeAnimatorController = Controller(male, scratch);
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                PrefabUtility.SaveAsPrefabAsset(scratch, male ? MalePrefab : FemalePrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(scratch); }
            return catalog;
        }

        public static Appearance DefaultAppearance(CharacterCatalog catalog)
        {
            return new Appearance { catalogId = catalog.id, torso = catalog.label == "Male" ? 3 : 2, pants = 2, footwear = 1, hair = 1, brows = 1, beard = catalog.beards.Length > 0 ? 1 : 0, leftGlove = 0, rightGlove = 0 };
        }

        private static void PopulateFemale(CharacterCatalog c, Dictionary<string, PartDefinition> parts, Action<string, string> label)
        {
            var audited = FemaleCustomizationSetup.BuildCatalog();
            c.headParts = A("CHR_Female_Head"); c.bareFeetParts = A("CHR_Legs");
            label("CHR_Female_Head", "Head & skin"); label("CHR_Legs", "Bare feet");
            label("CHR_Hand_L", "Left exposed fingers"); label("CHR_Hand_R", "Right exposed fingers");
            c.torsos = new TorsoOption[4]; c.pants = new PantsOption[4]; c.gloves = new GloveOption[4];
            for (int i = 0; i < 4; i++)
            {
                string letter = L(i), torso = i == 0 ? "CHR_Frmale_Torso_A" : "CHR_Female_Torso_" + letter;
                c.torsos[i] = new TorsoOption { id = letter, label = "Outfit " + letter, parts = A(torso), leftArm = FemaleArm(letter, "L", label), rightArm = FemaleArm(letter, "R", label) };
                label(torso, "Torso " + letter);
                string longPants = "CHR_Female_Pant_" + letter + "_01", shortPants = "CHR_Female_Pant_" + letter + "_02";
                c.pants[i] = new PantsOption { id = letter, label = "Trousers " + letter, longParts = A(longPants), shortParts = A(shortPants) };
                label(longPants, "Trousers " + letter + " - long"); label(shortPants, "Trousers " + letter + " - short");
                string left = "CHR_Glove_" + letter + "_L", right = "CHR_Glove_" + letter + "_R";
                c.gloves[i] = new GloveOption { id = letter, label = "Glove " + letter + (i == 1 ? " - fingerless" : ""), leftParts = A(left), rightParts = A(right) };
                label(left, "Left glove " + letter); label(right, "Right glove " + letter);
            }
            parts["CHR_Torso_D_Hand_L_03"].mesh = audited.torsos[3].leftArm.forearm.mesh;
            parts["CHR_Torso_D_Hand_R_03"].mesh = audited.torsos[3].rightArm.forearm.mesh;
            c.gloves[1].leftCompanionPartId = "CHR_Hand_L"; c.gloves[1].rightCompanionPartId = "CHR_Hand_R";
            c.gloves[1].leftCompanionMesh = audited.gloves[1].leftHandCompanion; c.gloves[1].rightCompanionMesh = audited.gloves[1].rightHandCompanion;
            c.footwear = Enumerable.Range(0, 6).Select(i => { string id = "SM_Boots_" + L(i); label(id, (i < 4 ? "Tall boots " : "Low shoes ") + L(i)); return new FootwearOption { id = L(i), label = (i < 4 ? "Tall boots " : "Low shoes ") + L(i), parts = A(id), high = i < 4 }; }).ToArray();
            c.hair = Enumerable.Range(0, 3).Select(i => Option("hair-" + i, "Hair " + L(i + 3), "SM_Hair_" + L(i + 3) + "_01", label)).ToArray();
            c.brows = Enumerable.Range(0, 3).Select(i => Option("brows-" + i, "Eyebrows " + L(i), "SM_Female_Brows_" + L(i), label)).ToArray();
            c.beards = new CharacterOption[0];
        }

        private static ArmSet FemaleArm(string letter, string side, Action<string, string> label)
        {
            string prefix = "CHR_Torso_" + letter + "_Hand_" + side, human = side == "L" ? "Left " : "Right ";
            label(prefix + "_01", human + "whole arm " + letter); label(prefix + "_02", human + "upper sleeve " + letter); label(prefix + "_03", human + "forearm " + letter);
            return new ArmSet { full = A(prefix + "_01"), upper = A(prefix + "_02"), forearm = A(prefix + "_03") };
        }

        private static void PopulateMale(CharacterCatalog c, Dictionary<string, SkinnedMeshRenderer> source, Dictionary<string, PartDefinition> parts, Action<string, string> label)
        {
            string masks = "Assets/Animpic Studio/POLY-FantasyCharacter/Editor/Studio/Audit/";
            Mesh fingersL = FemaleFingerMeshBuilder.Build(source["Cube_026"].sharedMesh, true, masks + "MaleGloveBFingerMask.json", "Male_Glove_B_Fingers_L");
            Mesh fingersR = FemaleFingerMeshBuilder.Build(source["Cube_074"].sharedMesh, false, masks + "MaleGloveBFingerMask.json", "Male_Glove_B_Fingers_R");
            Mesh lowerR = FemaleFingerMeshBuilder.Build(source["Cube_145"].sharedMesh, false, masks + "MaleRightDForearmMask.json", "Male_D_Forearm_R");
            parts.Add(GeneratedForearm, Definition(source["Cube_145"], GeneratedForearm, "Right forearm D", lowerR));
            parts["Cube_129"].mesh = FemaleForearmMeshBuilder.Build(source["Cube_129"].sharedMesh, source["Cube_123"].sharedMesh, "Male_D_Forearm_L_Clean");
            c.headParts = A("Cube_144"); c.bareFeetParts = A("Cube_135");
            label("Cube_144", "Head & skin"); label("Cube_135", "Bare feet"); label("Cube_026", "Left exposed fingers"); label("Cube_074", "Right exposed fingers");
            string[] torsos = { "010", "078", "082", "121" };
            string[,] arms = { { "002", "016", "134", "006", "018", "137" }, { "095", "097", "140", "094", "096", "141" }, { "088", "100", "112", "099", "111", "113" }, { "123", "130", "129", "145", "156", "GENERATED" } };
            c.torsos = new TorsoOption[4];
            for (int i = 0; i < 4; i++)
            {
                string torso = "Cube_" + torsos[i]; label(torso, "Torso " + L(i));
                c.torsos[i] = new TorsoOption { id = L(i), label = "Outfit " + L(i), parts = A(torso), leftArm = MaleArm(arms, i, 0, label), rightArm = MaleArm(arms, i, 3, label) };
            }
            string[,] pants = { { "015", "003" }, { "063", "004" }, { "133", "132" }, { "118", "202" } };
            c.pants = new PantsOption[4];
            for (int i = 0; i < 4; i++)
            {
                string longId = "Cube_" + pants[i, 0], shortId = "Cube_" + pants[i, 1];
                label(longId, "Trousers " + L(i) + " - long"); label(shortId, "Trousers " + L(i) + " - short");
                c.pants[i] = new PantsOption { id = L(i), label = "Trousers " + L(i), longParts = A(longId), shortParts = A(shortId) };
            }
            string[] boots = { "011", "020", "182", "116", "193", "195" };
            c.footwear = boots.Select((id, i) => { id = "Cube_" + id; string title = (i < 4 ? "Tall boots " : "Low shoes ") + L(i); label(id, title); return new FootwearOption { id = L(i), label = title, parts = A(id), high = i < 4 }; }).ToArray();
            string[,] gloves = { { "017", "013" }, { "103", "104" }, { "186", "189" }, { "175", "176" } };
            c.gloves = new GloveOption[4];
            for (int i = 0; i < 4; i++)
            {
                string left = "Cube_" + gloves[i, 0], right = "Cube_" + gloves[i, 1]; label(left, "Left glove " + L(i)); label(right, "Right glove " + L(i));
                c.gloves[i] = new GloveOption { id = L(i), label = "Glove " + L(i) + (i == 1 ? " - fingerless" : ""), leftParts = A(left), rightParts = A(right) };
            }
            c.gloves[1].leftCompanionPartId = "Cube_026"; c.gloves[1].rightCompanionPartId = "Cube_074";
            c.gloves[1].leftCompanionMesh = fingersL; c.gloves[1].rightCompanionMesh = fingersR;
            c.hair = new[] { Option("hair-a", "Hair A - full", "Cube_008", label), Option("hair-a-cropped", "Hair A - cropped", "Cube_170", label), Option("hair-b", "Hair B - full", "Cube_012", label), Option("hair-b-cropped", "Hair B - cropped", "Cube_168", label), Option("hair-c", "Hair C - full", "Cube_090", label), Option("hair-c-cropped", "Hair C - cropped", "Cube_169", label) };
            c.brows = new[] { Option("brows-a", "Eyebrows A", "Cube_207", label), Option("brows-b", "Eyebrows B", "Cube_229", label), Option("brows-c", "Eyebrows C", "Cube_232", label) };
            c.beards = new[] { Option("beard-a", "Full beard", "Cube_023", label), Option("beard-b", "Outlined beard", "Cube_087", label), Option("beard-c", "Moustache & goatee", "Cube_092", label) };
        }

        private static ArmSet MaleArm(string[,] source, int style, int offset, Action<string, string> label)
        {
            var ids = Enumerable.Range(0, 3).Select(i => source[style, offset + i] == "GENERATED" ? GeneratedForearm : "Cube_" + source[style, offset + i]).ToArray();
            string side = offset == 0 ? "Left " : "Right ";
            label(ids[0], side + "whole arm " + L(style)); label(ids[1], side + "upper sleeve " + L(style)); label(ids[2], side + "forearm " + L(style));
            return new ArmSet { full = A(ids[0]), upper = A(ids[1]), forearm = A(ids[2]) };
        }
        private static CharacterOption Option(string id, string title, string part, Action<string, string> label) { label(part, title); return new CharacterOption { id = id, label = title, parts = A(part) }; }
        private static PartDefinition Definition(SkinnedMeshRenderer r, string id, string label, Mesh mesh) { return new PartDefinition { id = id, label = label, mesh = mesh, requiredBoneNames = r.bones.Select(b => b.name).ToArray(), requiredRootBoneName = r.rootBone.name }; }
        private static string[] A(string value) { return new[] { value }; }
        private static string L(int i) { return ((char)('A' + i)).ToString(); }

        private static void AddGeneratedForearm(GameObject root, Mesh mesh)
        {
            var source = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r => r.name == "Cube_145");
            var go = new GameObject(GeneratedForearm);
            go.transform.SetParent(source.transform.parent, false);
            go.transform.localPosition = source.transform.localPosition; go.transform.localRotation = source.transform.localRotation; go.transform.localScale = source.transform.localScale;
            var renderer = go.AddComponent<SkinnedMeshRenderer>(); renderer.sharedMesh = mesh; renderer.bones = source.bones; renderer.rootBone = source.rootBone;
            renderer.sharedMaterials = source.sharedMaterials; renderer.localBounds = source.localBounds; renderer.quality = source.quality;
            renderer.shadowCastingMode = source.shadowCastingMode; renderer.receiveShadows = source.receiveShadows;
        }

        private static Material[] Palettes()
        {
            var result = new Material[6];
            for (int i = 0; i < result.Length; i++)
            {
                string path = "Assets/Animpic Studio/POLY-FantasyCharacter/Materials/Studio/CharacterPalette_" + L(i) + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!material) { material = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(material, path); }
                material.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Animpic Studio/POLY-FantasyCharacter/Textures/T_Main_" + L(i) + ".png"));
                material.SetColor("_Color", Color.white); material.SetFloat("_Metallic", 0.04f); material.SetFloat("_Glossiness", 0.23f);
                EditorUtility.SetDirty(material); AssetDatabase.SaveAssetIfDirty(material); result[i] = material;
            }
            return result;
        }

        public static AnimationClip IdleClip(bool male)
        {
            string path = "Assets/Animpic Studio/POLY-FantasyCharacter/Meshes/" + (male ? "POLY_FantasyMale_SK" : "POLY_FantasyFemale_Unity") + ".fbx";
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => c.name.IndexOf("relax", StringComparison.OrdinalIgnoreCase) >= 0 && !c.name.StartsWith("__preview__"));
        }
        private static RuntimeAnimatorController Controller(bool male, GameObject root)
        {
            var clip = CharacterStudioSetup.CreateIdle(root, male);
            if (!clip) throw new InvalidOperationException("Audited idle animation is missing.");
            string path = "Assets/Animpic Studio/POLY-FantasyCharacter/Animations/Studio/" + (male ? "Male" : "Female") + "StudioIdle.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (!controller) controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            var machine = controller.layers[0].stateMachine;
            var state = machine.states.Length > 0 ? machine.states[0].state : machine.AddState("Idle");
            state.motion = clip; machine.defaultState = state;
            EditorUtility.SetDirty(controller); AssetDatabase.SaveAssetIfDirty(controller); return controller;
        }
        public static void Ensure(string path) { if (AssetDatabase.IsValidFolder(path)) return; string parent = path.Substring(0, path.LastIndexOf('/')); Ensure(parent); AssetDatabase.CreateFolder(parent, path.Substring(parent.Length + 1)); }
    }
}
