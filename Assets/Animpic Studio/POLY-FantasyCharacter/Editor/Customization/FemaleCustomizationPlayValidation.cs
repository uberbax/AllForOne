using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Animpic.FantasyCharacter.Editor
{
    [InitializeOnLoad]
    public static class FemaleCustomizationPlayValidation
    {
        private const string PendingKey = "Animpic.FemaleCustomization.PlayValidation";
        private static string ReportPath { get { return Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/AnimpicFemaleValidation/play-mode.txt")); } }
        static FemaleCustomizationPlayValidation() { EditorApplication.playModeStateChanged += StateChanged; }

        [MenuItem("Tools/Animpic Studio/Characters/Fantasy Character/Validate in Play mode", false, 300)]
        public static void Start()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Start this check from Edit mode.");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, "RUNNING: waiting for Play mode.");
            SessionState.SetBool(PendingKey, true);
            EditorApplication.isPlaying = true;
        }

        private static void StateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(PendingKey, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode) EditorApplication.delayCall += Run;
            if (state == PlayModeStateChange.EnteredEditMode) SessionState.SetBool(PendingKey, false);
        }

        private static void Run()
        {
            GameObject instance = null;
            try
            {
                if (!Application.isPlaying) throw new InvalidOperationException("Runtime check did not enter Play mode.");
                instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(FemaleCustomizationSetup.CustomPrefab));
                var character = instance.GetComponent<FemaleCharacterCustomizer>();
                if (!character) throw new InvalidOperationException("Missing runtime component.");
                var randomState = UnityEngine.Random.state;
                int bakedParts = 0;
                for (int seed = 0; seed < 40; seed++)
                {
                    string error;
                    if (!character.Randomize(seed, out error)) throw new InvalidOperationException(error);
                    string json = character.SaveJson();
                    if (!character.TryLoadJson(json, out error)) throw new InvalidOperationException(error);
                    foreach (var renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        if (!renderer.enabled) continue;
                        var baked = new Mesh();
                        try
                        {
                            renderer.BakeMesh(baked);
                            if (baked.vertexCount == 0) throw new InvalidOperationException(renderer.name + " produced no skinned vertices.");
                            foreach (var point in baked.vertices)
                                if (float.IsNaN(point.x) || float.IsInfinity(point.x) || float.IsNaN(point.y) || float.IsInfinity(point.y) || float.IsNaN(point.z) || float.IsInfinity(point.z)) throw new InvalidOperationException(renderer.name + " has non-finite skinned geometry.");
                            bakedParts++;
                        }
                        finally { Object.DestroyImmediate(baked); }
                    }
                }
                // The same seed must reproduce appearance without using Unity's global random source.
                string failure;
                character.Randomize(1234, out failure); string first = character.SaveJson();
                character.Randomize(1234, out failure);
                if (first != character.SaveJson()) throw new InvalidOperationException("Randomization is not reproducible.");
                if (!UnityEngine.Random.state.Equals(randomState)) throw new InvalidOperationException("Customization changed Unity's global random state.");
                List<FemaleMeshPart> parts;
                if (!character.Catalog.TryGetParts(out parts, out failure)) throw new InvalidOperationException(failure);
                int unreadable = 0;
                foreach (var part in parts) if (!part.mesh.isReadable) unreadable++;
                File.WriteAllText(ReportPath, "PASS\nApplication.isPlaying: true\n40 randomized appearances and JSON round trips\n" + bakedParts + " visible skinned meshes baked with finite vertices\n" + unreadable + " catalogue source meshes have Read/Write disabled\nOriginal global random state preserved\n" + DateTime.Now.ToString("O"));
            }
            catch (Exception e) { File.WriteAllText(ReportPath, "FAIL\n" + e); Debug.LogException(e); }
            finally
            {
                if (instance) Object.Destroy(instance);
                EditorApplication.isPlaying = false;
            }
        }
    }
}
