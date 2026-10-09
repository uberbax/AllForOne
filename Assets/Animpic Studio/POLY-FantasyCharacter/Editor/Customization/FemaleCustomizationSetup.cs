using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Animpic.FantasyCharacter.Editor
{
    public static class FemaleCustomizationSetup
    {
        public const string AssetRoot = "Assets/Animpic Studio/POLY-FantasyCharacter";
        public const string SourcePrefab = "Assets/Animpic Studio/POLY-FantasyCharacter/Prefabs/POLY_FantasyFemale_Unity.prefab";
        public const string CustomPrefab = "Assets/Animpic Studio/POLY-FantasyCharacter/Prefabs/POLY_FantasyFemale_Customizable.prefab";
        public const string CatalogPath = "Assets/Animpic Studio/POLY-FantasyCharacter/Runtime/Customization/Data/FemaleCustomizationCatalog.asset";
        public const string DemoScene = "Assets/Animpic Studio/POLY-FantasyCharacter/Scenes/Demonstration.unity";
        private static string ProjectRoot { get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..")); } }
        private static string Output { get { return Path.Combine(ProjectRoot, "Library/AnimpicFemaleValidation"); } }

        [MenuItem("Tools/Animpic Studio/Characters/Fantasy Character/Set up customization", false, 300)]
        public static void SetupAndValidate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play mode before setup.");
            var catalog = BuildCatalog();
            var scratch = PrefabUtility.LoadPrefabContents(SourcePrefab);
            try
            {
                Configure(scratch, catalog);
                PrefabUtility.SaveAsPrefabAsset(scratch, CustomPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(scratch); }
            ValidateAll();

            var scene = SceneManager.GetActiveScene();
            if (scene.path == DemoScene)
            {
                var candidates = scene.GetRootGameObjects().Where(g => g.name == "POLY_FantasyFemale_Unity").ToArray();
                if (candidates.Length != 1) throw new InvalidOperationException("Expected one POLY_FantasyFemale_Unity root in Demonstration; prefab was created, scene has not been changed.");
                Directory.CreateDirectory(Output);
                string backup = Path.Combine(Output, "Demonstration-before-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity");
                if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new IOException("Could not save a copy of the current scene before customization.");
                var root = candidates[0];
                Undo.RegisterFullObjectHierarchyUndo(root, "Set up female customization");
                Configure(root, catalog, true);
                foreach (var r in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (PrefabUtility.IsPartOfPrefabInstance(r)) PrefabUtility.RecordPrefabInstancePropertyModifications(r);
                    if (PrefabUtility.IsPartOfPrefabInstance(r.gameObject)) PrefabUtility.RecordPrefabInstancePropertyModifications(r.gameObject);
                }
                var customizer = root.GetComponent<FemaleCharacterCustomizer>();
                if (PrefabUtility.IsPartOfPrefabInstance(customizer)) PrefabUtility.RecordPrefabInstancePropertyModifications(customizer);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Customization configured but the scene could not be saved.");
                Selection.activeGameObject = root;
                if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.Frame(new Bounds(root.transform.position + Vector3.up, new Vector3(2.2f, 2.2f, 1)), false);
            }
            AssetDatabase.SaveAssetIfDirty(catalog);
            RenderExamples();
            Debug.Log("Fantasy Female customization is ready. Select the female root to use the Inspector. Validation: " + Output);
        }

        public static FemaleCustomizationCatalog BuildCatalog()
        {
            EnsureFolder("Assets/Animpic Studio/POLY-FantasyCharacter/Runtime/Customization/Data");
            EnsureFolder("Assets/Animpic Studio/POLY-FantasyCharacter/Meshes/Customization");
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefab);
            if (!source) throw new InvalidOperationException("Female source prefab is missing.");
            var renderers = source.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length != 60) throw new InvalidOperationException("This catalogue was audited for 60 parts. Re-audit the changed source before rebuilding.");
            var meshes = renderers.ToDictionary(r => r.name, r => r.sharedMesh, StringComparer.Ordinal);
            var catalog = AssetDatabase.LoadAssetAtPath<FemaleCustomizationCatalog>(CatalogPath);
            bool created = !catalog;
            if (created) catalog = ScriptableObject.CreateInstance<FemaleCustomizationCatalog>();
            var sourceBindings = renderers.ToDictionary(r => r.name, StringComparer.Ordinal);
            Func<string, FemaleMeshPart> part = name => new FemaleMeshPart { rendererName = name, mesh = meshes[name], requiredBoneNames = sourceBindings[name].bones.Select(b => b.name).ToArray(), requiredRootBoneName = sourceBindings[name].rootBone.name };
            catalog.head = part("CHR_Female_Head"); catalog.feet = part("CHR_Legs");
            catalog.leftHand = part("CHR_Hand_L"); catalog.rightHand = part("CHR_Hand_R");
            catalog.torsos = new FemaleTorsoOption[4]; catalog.pants = new FemalePantsOption[4];
            catalog.gloves = new FemaleGloveOption[4]; catalog.footwear = new FemaleFootwearOption[6];
            for (int i = 0; i < 4; i++)
            {
                string style = ((char)('A' + i)).ToString();
                catalog.torsos[i] = new FemaleTorsoOption {
                    torso = part(i == 0 ? "CHR_Frmale_Torso_A" : "CHR_Female_Torso_" + style),
                    leftArm = Arm(style, "L", part), rightArm = Arm(style, "R", part)
                };
                catalog.pants[i] = new FemalePantsOption { longPants = part("CHR_Female_Pant_" + style + "_01"), shortPants = part("CHR_Female_Pant_" + style + "_02") };
                catalog.gloves[i] = new FemaleGloveOption {
                    left = part("CHR_Glove_" + style + "_L"), right = part("CHR_Glove_" + style + "_R"),
                    handCoverage = i == 1 ? FemaleHandCoverage.BareHandRequired : FemaleHandCoverage.ReplacesHand,
                    leftHandCompanion = i == 1 ? FemaleFingerMeshBuilder.Build(meshes["CHR_Hand_L"], true) : null,
                    rightHandCompanion = i == 1 ? FemaleFingerMeshBuilder.Build(meshes["CHR_Hand_R"], false) : null
                };
            }
            // D_03 contains the same surface twice in the source FBX. Keep one copy,
            // without changing source data, skinning indices, UVs or blend shapes.
            catalog.torsos[3].leftArm.forearm.mesh = FemaleForearmMeshBuilder.Build(meshes["CHR_Torso_D_Hand_L_03"], meshes["CHR_Torso_D_Hand_L_01"], "CHR_Torso_D_Hand_L_03_Clean");
            catalog.torsos[3].rightArm.forearm.mesh = FemaleForearmMeshBuilder.Build(meshes["CHR_Torso_D_Hand_R_03"], meshes["CHR_Torso_D_Hand_R_01"], "CHR_Torso_D_Hand_R_03_Clean");
            catalog.palettes = new Material[6];
            for (int i = 0; i < 6; i++)
            {
                string style = ((char)('A' + i)).ToString();
                catalog.footwear[i] = new FemaleFootwearOption { part = part("SM_Boots_" + style), high = i < 4 };
                catalog.palettes[i] = AssetDatabase.LoadAssetAtPath<Material>("Assets/Animpic Studio/POLY-FantasyCharacter/Materials/M_Main_" + style + ".mat");
            }
            catalog.hair = new[] { part("SM_Hair_D_01"), part("SM_Hair_E_01"), part("SM_Hair_F_01") };
            catalog.brows = new[] { part("SM_Female_Brows_A"), part("SM_Female_Brows_B"), part("SM_Female_Brows_C") };
            List<FemaleMeshPart> parts;
            string error;
            if (!catalog.TryGetParts(out parts, out error)) throw new InvalidOperationException(error);
            if (parts.Count != 60) throw new InvalidOperationException("Catalogue does not cover all 60 parts.");
            if (created) AssetDatabase.CreateAsset(catalog, CatalogPath);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            return catalog;
        }

        private static FemaleArmSet Arm(string style, string side, Func<string, FemaleMeshPart> part)
        {
            string prefix = "CHR_Torso_" + style + "_Hand_" + side;
            return new FemaleArmSet { full = part(prefix + "_01"), upper = part(prefix + "_02"), forearm = part(prefix + "_03") };
        }

        public static FemaleCharacterCustomizer Configure(GameObject root, FemaleCustomizationCatalog catalog, bool undo = false)
        {
            foreach (var r in root.GetComponentsInChildren<SkinnedMeshRenderer>(true)) r.gameObject.SetActive(true);
            var character = root.GetComponent<FemaleCharacterCustomizer>();
            bool existed = character != null;
            if (!character) character = undo ? Undo.AddComponent<FemaleCharacterCustomizer>(root) : root.AddComponent<FemaleCharacterCustomizer>();
            var selection = existed ? character.CaptureSelection() : new FemaleCustomizationSelection { footwear = 1 };
            character.Configure(catalog);
            string error;
            if (!character.TryApply(selection, out error)) throw new InvalidOperationException(error);
            return character;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = path.Substring(0, path.LastIndexOf('/'));
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
        }

        [MenuItem("Tools/Animpic Studio/Characters/Fantasy Character/Validate customization", false, 300)]
        public static void ValidateAll() { FemaleCustomizationValidation.Run(CustomPrefab, Output); }

        [MenuItem("Tools/Animpic Studio/Characters/Fantasy Character/Render examples", false, 300)]
        public static void RenderExamples() { FemaleCustomizationValidation.Render(CustomPrefab, Output); }
        public static void ValidatePlayMode() { FemaleCustomizationPlayValidation.Start(); }
    }
}
