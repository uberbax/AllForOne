using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Animpic.CharacterStudio.Editor
{
    [InitializeOnLoad]
    public static class CharacterStudioPlayValidation
    {
        private const string Pending = "Animpic.CharacterStudio.PlayValidationPending";
        private static string Output { get { return Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/CharacterStudioValidation/play-mode.json")); } }
        [Serializable] private sealed class Report
        {
            public bool passed, applicationIsPlaying, runtimeUiValidated; public string status, completedUtc, failure, screenshotPath;
            public int models, applications, roundTrips, bakedMeshes, unreadableMeshes, assertions;
        }
        static CharacterStudioPlayValidation() { EditorApplication.playModeStateChanged += StateChanged; }
        [MenuItem("Tools/Animpic Studio/Characters/Fantasy Character/Validation/Character Studio in Play Mode", false, 300)]
        public static void Start()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Start Play validation from Edit mode.");
            Directory.CreateDirectory(Path.GetDirectoryName(Output));
            File.WriteAllText(Output, JsonUtility.ToJson(new Report { status = "waiting_for_play_mode" }, true));
            SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true;
        }
        private static void StateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Pending, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode) EditorApplication.delayCall += Run;
            if (state == PlayModeStateChange.EnteredEditMode) SessionState.SetBool(Pending, false);
        }
        private static void Check(Report report, bool ok, string error)
        {
            report.assertions++; if (!ok) throw new InvalidOperationException(error);
        }
        private static void Run()
        {
            var report = new Report { applicationIsPlaying = Application.isPlaying, status = "running" };
            try
            {
                Check(report, Application.isPlaying, "Not in actual Play mode.");
                ValidateModel(false, report); ValidateModel(true, report);
                var uiType = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Animpic.CharacterStudio.Editor.CharacterStudioUIValidation")).FirstOrDefault(t => t != null);
                var method = uiType == null ? null : uiType.GetMethod("ValidateRuntimeUI", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                Check(report, method != null, "Runtime UI validation helper is missing.");
                method.Invoke(null, null); report.runtimeUiValidated = true;
                var ui = Object.FindObjectOfType<CharacterStudioUI>();
                Check(report, ui != null, "Active studio UI is missing after runtime validation.");
                report.screenshotPath = Path.Combine(Path.GetDirectoryName(Output), "studio-play.png");
                CharacterStudioSetup.Render(ui, report.screenshotPath);
                Check(report, File.Exists(report.screenshotPath), "Play Mode studio screenshot was not written.");
                report.passed = true; report.status = "passed";
            }
            catch (Exception e) { report.status = "failed"; report.failure = e.ToString(); Debug.LogException(e); }
            finally { report.completedUtc = DateTime.UtcNow.ToString("O"); File.WriteAllText(Output, JsonUtility.ToJson(report, true)); EditorApplication.isPlaying = false; }
        }
        private static void ValidateModel(bool male, Report report)
        {
            string path = "Assets/Animpic Studio/POLY-FantasyCharacter/Prefabs/" + (male ? "POLY_FantasyMale_Studio" : "POLY_FantasyFemale_Studio") + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path); Check(report, prefab != null, "Missing studio prefab: " + path);
            var instance = Object.Instantiate(prefab); instance.name = "__StudioPlayValidation"; instance.hideFlags = HideFlags.None;
            try
            {
                var c = instance.GetComponent<CharacterCustomizer>(); Check(report, c != null && c.Catalog != null, "Missing runtime customization component/catalog.");
                var catalog = c.Catalog; var animator = instance.GetComponent<Animator>();
                if (animator) { animator.Update(0.15f); animator.enabled = false; }
                var transforms = instance.GetComponentsInChildren<Transform>(true);
                var positions = transforms.Select(t => t.localPosition).ToArray(); var rotations = transforms.Select(t => t.localRotation).ToArray();
                var scales = transforms.Select(t => t.localScale).ToArray(); var active = transforms.Select(t => t.gameObject.activeSelf).ToArray();
                var renderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                var bones = renderers.Select(r => r.bones).ToArray(); var rootBones = renderers.Select(r => r.rootBone).ToArray();
                var paletteStates = catalog.palettes.Select(m => EditorJsonUtility.ToJson(m)).ToArray();
                int materialsBefore = Resources.FindObjectsOfTypeAll<Material>().Length; var randomState = UnityEngine.Random.state;
                foreach (var p in catalog.parts) if (!p.mesh.isReadable) report.unreadableMeshes++;
                for (int seed = 0; seed < 32; seed++)
                {
                    Check(report, c.Randomize(seed, out var error), "Runtime randomization failed: " + error); report.applications++;
                    var visible = c.GetVisibleParts(); Check(report, visible.Length > 0, "Runtime customization rendered nothing.");
                    var target = visible[seed % visible.Length]; var a = c.Capture();
                    a.paints = new[] { new PartPaint { partId = target.id, palette = 4, tint = new Color(0.3f, 0.6f, 0.9f, 1) } };
                    Check(report, c.TryApply(a, out error), "Runtime tint application failed: " + error); report.applications++;
                    foreach (var renderer in renderers)
                    {
                        var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block);
                        Color expected = renderer.name == target.id ? a.paints[0].tint : Color.white;
                        Check(report, Vector4.Distance(block.GetColor("_Color"), expected) < 0.00001f, "Per-part color leaked or disappeared in Play mode.");
                        Check(report, renderer.sharedMaterials.All(m => m == catalog.palettes[renderer.name == target.id ? 4 : a.basePalette]), "Per-part palette isolation failed in Play mode.");
                    }
                    string json = c.SaveJson(); Check(report, c.TryLoadJson(json, out error) && c.SaveJson() == json, "Runtime JSON round trip failed: " + error); report.roundTrips++; report.applications++;
                    Check(report, !c.TryLoadJson("{\"schemaVersion\":1,\"appearance\":null}", out error) && c.SaveJson() == json, "Null JSON changed a runtime appearance.");
                    if (seed % 4 == 0) foreach (var p in visible)
                    {
                        var mesh = new Mesh();
                        try
                        {
                            p.renderer.BakeMesh(mesh); Check(report, mesh.vertexCount > 0, "Runtime skinning produced an empty mesh.");
                            foreach (var v in mesh.vertices) Check(report, Finite(v.x) && Finite(v.y) && Finite(v.z), "Runtime skinning produced nonfinite coordinates.");
                            report.bakedMeshes++;
                        }
                        finally { Object.DestroyImmediate(mesh); }
                    }
                    for (int i = 0; i < transforms.Length; i++) Check(report, transforms[i].localPosition == positions[i] && transforms[i].localRotation == rotations[i] && transforms[i].localScale == scales[i] && transforms[i].gameObject.activeSelf == active[i], "Runtime customization altered rig pose or activation.");
                    for (int i = 0; i < renderers.Length; i++) Check(report, renderers[i].rootBone == rootBones[i] && renderers[i].bones.SequenceEqual(bones[i]), "Runtime customization rebound bones.");
                }
                Check(report, c.Randomize(55, out var lastError), lastError); string deterministic = c.SaveJson();
                Check(report, c.Randomize(55, out lastError) && c.SaveJson() == deterministic, "Runtime randomization is not deterministic.");
                Check(report, UnityEngine.Random.state.Equals(randomState), "Runtime customization consumed Unity's global random sequence.");
                for (int i = 0; i < catalog.palettes.Length; i++) Check(report, EditorJsonUtility.ToJson(catalog.palettes[i]) == paletteStates[i], "A shared palette material was changed.");
                Check(report, Resources.FindObjectsOfTypeAll<Material>().Length == materialsBefore, "Runtime customization leaked Material instances."); report.models++;
            }
            finally { Object.DestroyImmediate(instance); }
        }
        private static bool Finite(float n) { return !float.IsNaN(n) && !float.IsInfinity(n); }
    }
}