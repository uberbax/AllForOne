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
    /// <summary>Checks isolated prefab instances; never writes the source prefab or open scene.</summary>
    public static class FemaleCustomizationValidation
    {
        [Serializable] private sealed class Report
        {
            public string prefabPath, completedUtc, lastCase, failure;
            public bool passed;
            public int assertions, applications, lowerBodyCases, armCases, gloveRemovalCases, independentGloves,
                appearanceCases, idempotenceCases, jsonRoundTrips, invalidInputs, invalidConfigurations, skinMeshes;
        }
        private sealed class Pose
        {
            public Transform t, parent;
            public Vector3 position, scale;
            public Quaternion rotation;
            public bool active;
        }
        private sealed class Visual
        {
            public SkinnedMeshRenderer r;
            public Mesh mesh;
            public Material[] materials;
            public Transform[] bones;
            public Transform rootBone;
            public bool enabled;
        }
        private sealed class Fixture
        {
            public GameObject root;
            public FemaleCharacterCustomizer c;
            public FemaleCustomizationCatalog catalog;
            public Dictionary<string, SkinnedMeshRenderer> meshes;
            public Pose[] pose;
            public Visual[] original;
            public Report report;
            public void Check(bool ok, string error) { report.assertions++; if (!ok) throw new InvalidOperationException(report.lastCase + ": " + error); }
            public void Hierarchy()
            {
                foreach (var p in pose)
                {
                    Check(p.t != null && p.t.parent == p.parent, "Missing or reparented transform.");
                    Check(p.t.gameObject.activeSelf == p.active, "Activation changed: " + p.t.name);
                    Check(p.t.localPosition == p.position && p.t.localRotation == p.rotation && p.t.localScale == p.scale, "Pose changed: " + p.t.name);
                }
                foreach (var v in original) Check(v.r.rootBone == v.rootBone && v.r.bones.SequenceEqual(v.bones), "Bone binding changed: " + v.r.name);
            }
            public void Same(Visual[] before)
            {
                foreach (var v in before) Check(v.r.enabled == v.enabled && v.r.sharedMesh == v.mesh && v.r.sharedMaterials.SequenceEqual(v.materials), "Visual state changed: " + v.r.name);
                Hierarchy();
            }
            public void Apply(FemaleCustomizationSelection s, string name)
            {
                report.lastCase = name;
                string error;
                Check(c.TryApply(s, out error), "TryApply failed: " + error);
                report.applications++;
                Check(c.LastValidationError == null, "Successful apply retained an error.");
                Verify(s);
            }
            public void Verify(FemaleCustomizationSelection s)
            {
                // These names and classifications are independently fixed by the audited source inventory.
                var expected = new HashSet<string>(StringComparer.Ordinal) { "CHR_Female_Head", s.torso == 0 ? "CHR_Frmale_Torso_A" : "CHR_Female_Torso_" + Style(s.torso) };
                expected.Add("CHR_Female_Pant_" + Style(s.pants) + (s.footwear > 0 && s.footwear < 5 ? "_02" : "_01"));
                expected.Add(s.footwear == 0 ? "CHR_Legs" : "SM_Boots_" + Style(s.footwear - 1));
                if (s.hair > 0) expected.Add("SM_Hair_" + Style(s.hair + 2) + "_01");
                if (s.brows > 0) expected.Add("SM_Female_Brows_" + Style(s.brows - 1));
                ExpectedArm(expected, s, true); ExpectedArm(expected, s, false);
                foreach (var name in expected) Check(meshes.ContainsKey(name), "Missing audited binding: " + name);
                foreach (var pair in meshes)
                {
                    var r = pair.Value;
                    Check(r.enabled == expected.Contains(pair.Key), "Wrong visibility: " + pair.Key);
                    Check(r.sharedMesh != null && r.sharedMaterials.Length == r.sharedMesh.subMeshCount, "Mesh/material layout changed: " + pair.Key);
                    foreach (var mat in r.sharedMaterials) Check(mat == catalog.palettes[s.palette], "Wrong palette: " + pair.Key);
                }
                Check(Json(c.CaptureSelection()) == Json(s), "Resolved equipment overwrote the logical selection.");
                foreach (bool left in new[] { true, false })
                {
                    int glove = left ? s.leftGlove : s.rightGlove;
                    var hand = meshes[left ? "CHR_Hand_L" : "CHR_Hand_R"];
                    Check(hand.enabled == (glove == 2), "Only fingerless glove B needs the separate bare hand.");
                    if (glove == 2) Check(hand.sharedMesh == (left ? catalog.gloves[1].leftHandCompanion : catalog.gloves[1].rightHandCompanion), "Wrong fingerless glove companion.");
                }
                Hierarchy();
            }
        }
        public static void Run(string prefabPath, string outputDir)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Run validation outside Play mode.");
            Directory.CreateDirectory(outputDir);
            var report = new Report { prefabPath = prefabPath, lastCase = "Load isolated prefab" };
            GameObject root = null;
            Exception failure = null;
            try
            {
                root = PrefabUtility.LoadPrefabContents(prefabPath);
                var f = new Fixture { root = root, report = report, c = root.GetComponent<FemaleCharacterCustomizer>() };
                f.Check(f.c != null && f.c.Catalog != null, "Customizer or catalogue is missing.");
                f.catalog = f.c.Catalog;
                var renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                f.Check(renderers.Length == 60, "Expected all 60 audited parts.");
                f.meshes = renderers.ToDictionary(r => r.name, StringComparer.Ordinal);
                f.pose = CapturePose(root); f.original = CaptureVisual(renderers);
                CheckSkin(f);
                for (int t = 0; t < 4; t++) for (int p = 0; p < 4; p++) for (int b = 0; b < 7; b++)
                { f.Apply(new FemaleCustomizationSelection { torso = t, pants = p, footwear = b }, "Lower body " + t + "/" + p + "/" + b); report.lowerBodyCases++; }
                foreach (bool left in new[] { true, false }) for (int t = 0; t < 4; t++) for (int m = 0; m < 2; m++) for (int a = -1; a < 4; a++) for (int g = 0; g < 5; g++)
                {
                    var s = new FemaleCustomizationSelection { torso = t, footwear = 5 };
                    SetArm(s, left, (FemaleArmMode)m, a, g); SetArm(s, !left, FemaleArmMode.Split, (t + 2) % 4, 0);
                    f.Apply(s, "Arm " + left + "/" + t + "/" + m + "/" + a + "/" + g); report.armCases++;
                    if (g > 0)
                    {
                        var restored = f.c.CaptureSelection();
                        if (left) { restored.leftGlove = 0; s.leftGlove = 0; } else { restored.rightGlove = 0; s.rightGlove = 0; }
                        f.Apply(restored, "Restore arm after glove removal"); f.Check(Json(restored) == Json(s), "Glove removal lost the selected arm variant."); report.gloveRemovalCases++;
                    }
                }
                for (int t = 0; t < 4; t++) for (int l = 0; l < 5; l++) for (int r = 0; r < 5; r++)
                {
                    f.Apply(new FemaleCustomizationSelection { torso = t, leftArmMode = FemaleArmMode.Split, leftForearmStyle = (t + 1) % 4, leftGlove = l, rightGlove = r }, "Independent gloves " + t + "/" + l + "/" + r);
                    report.independentGloves++;
                }
                for (int p = 0; p < 6; p++) for (int h = 0; h < 4; h++) for (int b = 0; b < 4; b++)
                { f.Apply(new FemaleCustomizationSelection { palette = p, hair = h, brows = b, leftGlove = 2, rightGlove = 4 }, "Appearance " + p + "/" + h + "/" + b); report.appearanceCases++; }
                foreach (var s in Examples())
                {
                    f.Apply(s, "Idempotence baseline"); var before = CaptureVisual(renderers); f.Apply(s, "Idempotence repeated apply"); f.Same(before); report.idempotenceCases++;
                    string saved = f.c.SaveJson(); f.Apply(new FemaleCustomizationSelection(), "Temporary JSON replacement");
                    string error; f.report.lastCase = "JSON round trip"; f.Check(f.c.TryLoadJson(saved, out error), "JSON reload failed: " + error);
                    f.Verify(s); f.Same(before); f.Check(f.c.SaveJson() == saved, "JSON round trip changed data."); report.jsonRoundTrips++;
                }
                CheckInvalid(f);
                f.Check(report.lowerBodyCases == 112 && report.armCases == 400 && report.gloveRemovalCases == 320 && report.independentGloves == 100 && report.appearanceCases == 96, "Incomplete combination matrices.");
                report.passed = true;
            }
            catch (Exception ex) { failure = ex; report.failure = ex.ToString(); }
            finally
            {
                if (root) PrefabUtility.UnloadPrefabContents(root);
                report.completedUtc = DateTime.UtcNow.ToString("O");
                string json = JsonUtility.ToJson(report, true);
                File.WriteAllText(Path.Combine(outputDir, "customization-validation.json"), json);
                File.WriteAllText(Path.Combine(outputDir, "customization-validation.txt"), (report.passed ? "PASS" : "FAIL") + " - Female customization\n" + json + "\nChecks use isolated prefab instances. Visual review of animated seams is also required.\n");
            }
            if (failure != null) throw new InvalidOperationException("Female validation failed; see " + Path.Combine(outputDir, "customization-validation.txt"), failure);
            Debug.Log("Female customization passed " + report.assertions + " assertions / " + report.applications + " applications. " + outputDir);
        }
        private static void CheckSkin(Fixture f)
        {
            f.report.lastCase = "Skin and catalogue bindings";
            List<FemaleMeshPart> parts; string error;
            f.Check(f.catalog.TryGetParts(out parts, out error) && parts.Count == 60, "Invalid catalogue: " + error);
            for (int i = 0; i < 6; i++) f.Check(f.catalog.footwear[i].high == (i < 4), "Wrong audited footwear height: " + Style(i));
            for (int i = 0; i < 4; i++) f.Check(f.catalog.gloves[i].handCoverage == (i == 1 ? FemaleHandCoverage.BareHandRequired : FemaleHandCoverage.ReplacesHand), "Wrong glove coverage: " + Style(i));
            var pairs = parts.Select(p => new KeyValuePair<SkinnedMeshRenderer, Mesh>(f.meshes[p.rendererName], p.mesh)).ToList();
            pairs.Add(new KeyValuePair<SkinnedMeshRenderer, Mesh>(f.meshes["CHR_Hand_L"], f.catalog.gloves[1].leftHandCompanion));
            pairs.Add(new KeyValuePair<SkinnedMeshRenderer, Mesh>(f.meshes["CHR_Hand_R"], f.catalog.gloves[1].rightHandCompanion));
            foreach (var pair in pairs)
            {
                var mesh = pair.Value; var bones = pair.Key.bones;
                f.Check(mesh != null && mesh.vertexCount > 0, "Missing mesh on " + pair.Key.name);
                var poses = mesh.bindposes;
                f.Check(poses.Length > 0 && poses.Length == bones.Length, "Bone/bindpose mismatch on " + pair.Key.name);
                for (int i = 0; i < bones.Length; i++)
                {
                    f.Check(bones[i] != null && bones[i].IsChildOf(f.root.transform), "External/missing bone on " + pair.Key.name);
                    for (int k = 0; k < 16; k++) f.Check(!float.IsNaN(poses[i][k]) && !float.IsInfinity(poses[i][k]), "Invalid bindpose.");
                }
                if (pair.Key.rootBone) f.Check(pair.Key.rootBone.IsChildOf(f.root.transform), "External root bone.");
                var weights = mesh.boneWeights;
                f.Check(weights.Length == mesh.vertexCount, "Missing vertex weights on " + mesh.name);
                foreach (var w in weights)
                {
                    f.Check(Mathf.Abs(w.weight0 + w.weight1 + w.weight2 + w.weight3 - 1f) < 0.002f, "Unnormalized skin weights on " + mesh.name);
                    f.Check(Weight(w.weight0, w.boneIndex0, bones.Length) && Weight(w.weight1, w.boneIndex1, bones.Length) && Weight(w.weight2, w.boneIndex2, bones.Length) && Weight(w.weight3, w.boneIndex3, bones.Length), "Invalid bone index on " + mesh.name);
                }
                f.report.skinMeshes++;
            }
        }
        private static bool Weight(float w, int i, int n) { return !float.IsNaN(w) && !float.IsInfinity(w) && w >= 0 && (w == 0 || (i >= 0 && i < n)); }
        private static void CheckInvalid(Fixture f)
        {
            var choice = f.c.CaptureSelection(); string saved = f.c.SaveJson(); var before = CaptureVisual(f.meshes.Values);
            var invalid = new List<FemaleCustomizationSelection> { null };
            foreach (var field in typeof(FemaleCustomizationSelection).GetFields())
            {
                if (field.FieldType == typeof(int)) foreach (int v in new[] { int.MinValue, int.MaxValue }) { var s = choice.Copy(); field.SetValue(s, v); invalid.Add(s); }
                else if (field.FieldType == typeof(FemaleArmMode)) { var s = choice.Copy(); field.SetValue(s, (FemaleArmMode)99); invalid.Add(s); }
            }
            foreach (var s in invalid)
            {
                f.report.lastCase = "Invalid selection must not mutate"; string error;
                f.Check(!f.c.TryApply(s, out error) && !string.IsNullOrEmpty(error), "Invalid input accepted.");
                f.Check(f.c.SaveJson() == saved, "Invalid input changed the selection."); f.Same(before); f.report.invalidInputs++;
            }
            foreach (string json in new[] { null, "", " ", "not json", "[]", "{", "{}", "{\"schemaVersion\":2,\"selection\":{}}", "{\"schemaVersion\":1}", "{\"schemaVersion\":1,\"selection\":null}", "{\"schemaVersion\":1,\"selection\":{\"footwear\":999}}" })
            {
                f.report.lastCase = "Invalid JSON must not mutate"; string error;
                f.Check(!f.c.TryLoadJson(json, out error) && !string.IsNullOrEmpty(error), "Invalid JSON accepted.");
                f.Check(f.c.SaveJson() == saved, "Invalid JSON changed the selection."); f.Same(before); f.report.invalidInputs++;
            }
            var clone = Object.Instantiate(f.catalog);
            try
            {
                clone.palettes = (Material[])f.catalog.palettes.Clone(); clone.palettes[0] = null; f.c.Configure(clone);
                f.report.lastCase = "Invalid catalogue must not mutate"; string error;
                f.Check(!f.c.TryApply(choice, out error), "Missing material accepted."); f.Same(before); f.Check(f.c.SaveJson() == saved, "Invalid catalogue changed selection."); f.report.invalidConfigurations++;
            }
            finally { f.c.Configure(f.catalog); Object.DestroyImmediate(clone); }
            var mesh = f.meshes["CHR_Legs"]; string name = mesh.name;
            try
            {
                mesh.name = "Validation_MissingBinding"; f.report.lastCase = "Missing binding must not mutate"; string error;
                f.Check(!f.c.TryApply(choice, out error), "Missing renderer accepted."); f.Same(before); f.Check(f.c.SaveJson() == saved, "Missing renderer changed selection."); f.report.invalidConfigurations++;
            }
            finally { mesh.name = name; }
            f.Apply(choice, "Recovery and selection ownership");
            var copied = f.c.CaptureSelection(); copied.torso = 999; f.Check(f.c.CaptureSelection().torso == choice.torso, "CaptureSelection leaked internal state.");
            choice.torso = 999; f.Check(f.c.CaptureSelection().torso != 999, "TryApply retained caller mutable selection.");
        }
        private static void ExpectedArm(HashSet<string> names, FemaleCustomizationSelection s, bool left)
        {
            string side = left ? "L" : "R", prefix = "CHR_Torso_" + Style(s.torso) + "_Hand_" + side;
            int glove = left ? s.leftGlove : s.rightGlove;
            if (glove > 0) { names.Add(prefix + "_02"); names.Add("CHR_Glove_" + Style(glove - 1) + "_" + side); if (glove == 2) names.Add("CHR_Hand_" + side); }
            else if ((left ? s.leftArmMode : s.rightArmMode) == FemaleArmMode.Full) names.Add(prefix + "_01");
            else { names.Add(prefix + "_02"); int style = left ? s.leftForearmStyle : s.rightForearmStyle; names.Add("CHR_Torso_" + Style(style < 0 ? s.torso : style) + "_Hand_" + side + "_03"); }
        }
        private static void SetArm(FemaleCustomizationSelection s, bool left, FemaleArmMode mode, int style, int glove)
        { if (left) { s.leftArmMode = mode; s.leftForearmStyle = style; s.leftGlove = glove; } else { s.rightArmMode = mode; s.rightForearmStyle = style; s.rightGlove = glove; } }
        private static string Style(int i) { return ((char)('A' + i)).ToString(); }
        private static string Json(FemaleCustomizationSelection s) { return JsonUtility.ToJson(s); }
        private static Pose[] CapturePose(GameObject root)
        { return root.GetComponentsInChildren<Transform>(true).Select(t => new Pose { t = t, parent = t.parent, position = t.localPosition, rotation = t.localRotation, scale = t.localScale, active = t.gameObject.activeSelf }).ToArray(); }
        private static Visual[] CaptureVisual(IEnumerable<SkinnedMeshRenderer> meshes)
        { return meshes.Select(r => new Visual { r = r, mesh = r.sharedMesh, materials = r.sharedMaterials, bones = r.bones, rootBone = r.rootBone, enabled = r.enabled }).ToArray(); }
        private static FemaleCustomizationSelection[] Examples()
        {
            return new[] {
                new FemaleCustomizationSelection { torso = 0, pants = 0, footwear = 0 },
                new FemaleCustomizationSelection { torso = 1, pants = 1, footwear = 1, leftGlove = 1, rightGlove = 1, palette = 1, hair = 2, brows = 2 },
                new FemaleCustomizationSelection { torso = 2, pants = 2, footwear = 5, leftArmMode = FemaleArmMode.Split, rightArmMode = FemaleArmMode.Split, leftForearmStyle = 0, rightForearmStyle = 3, palette = 2, hair = 3, brows = 3 },
                new FemaleCustomizationSelection { torso = 3, pants = 3, footwear = 4, leftGlove = 2, rightGlove = 2, palette = 3, hair = 1, brows = 2 },
                new FemaleCustomizationSelection { torso = 0, pants = 2, footwear = 6, leftArmMode = FemaleArmMode.Split, leftForearmStyle = 3, rightGlove = 3, palette = 4, hair = 2, brows = 0 },
                new FemaleCustomizationSelection { torso = 2, pants = 1, footwear = 2, leftGlove = 4, rightGlove = 2, palette = 5, hair = 0, brows = 1 }
            };
        }
        [Serializable] private sealed class RenderReport
        {
            public string prefabPath, animationClip, failure;
            public string materialMode = "Original catalogue materials";
            public bool passed;
            public List<string> images = new List<string>();
            public List<string> selections = new List<string>();
        }
        public static void Render(string prefabPath, string outputDir)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Render outside Play mode.");
            Directory.CreateDirectory(outputDir);
            var report = new RenderReport { prefabPath = prefabPath };
            var scene = EditorSceneManager.NewPreviewScene();
            RenderTexture target = null; Exception failure = null;
            try
            {
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (!source) throw new InvalidOperationException("Preview prefab is missing.");
                var root = (GameObject)PrefabUtility.InstantiatePrefab(source, scene);
                root.transform.position = Vector3.zero; root.transform.rotation = Quaternion.identity;
                foreach (var a in root.GetComponentsInChildren<Animator>(true)) a.enabled = false;
                foreach (var a in root.GetComponentsInChildren<Animation>(true)) a.enabled = false;
                var character = root.GetComponent<FemaleCharacterCustomizer>();
                if (!character) throw new InvalidOperationException("Preview has no customizer.");
                var pose = CapturePose(root);
                var cameraObject = new GameObject("Female customization preview camera"); SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.AddComponent<Camera>(); camera.scene = scene; camera.cameraType = CameraType.Preview; camera.enabled = false;
                camera.orthographic = true; camera.orthographicSize = 1.2f; camera.nearClipPlane = 0.05f; camera.farClipPlane = 30;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.15f, 0.17f, 0.20f, 1);
                camera.transform.position = new Vector3(2.8f, 1.7f, 4.2f); camera.transform.LookAt(new Vector3(0, 1, 0));
                Light(scene, "Key", new Vector3(45, -30, 0), 1.15f, new Color(1, 0.94f, 0.88f));
                Light(scene, "Fill", new Vector3(25, 135, 0), 0.75f, new Color(0.80f, 0.89f, 1));
                Light(scene, "Rim", new Vector3(145, 10, 0), 0.5f, Color.white);
                target = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4, hideFlags = HideFlags.HideAndDontSave }; target.Create(); camera.targetTexture = target;
                var clip = RelaxClip(); if (clip) report.animationClip = AssetDatabase.GetAssetPath(clip) + " :: " + clip.name;
                var selections = Examples();
                for (int i = 0; i < selections.Length; i++)
                {
                    foreach (var p in pose) { p.t.localPosition = p.position; p.t.localRotation = p.rotation; p.t.localScale = p.scale; p.t.gameObject.SetActive(p.active); }
                    string error; if (!character.TryApply(selections[i], out error)) throw new InvalidOperationException(error);
                    foreach (var r in root.GetComponentsInChildren<SkinnedMeshRenderer>(true)) { r.updateWhenOffscreen = true; r.localBounds = new Bounds(Vector3.up, new Vector3(4, 4, 4)); }
                    string path = Path.Combine(outputDir, "female-" + (i + 1).ToString("00") + "-bind.png"); WriteCamera(camera, target, path); report.images.Add(path); report.selections.Add(Json(selections[i]));
                    if (clip)
                    {
                        clip.SampleAnimation(root, Mathf.Min(clip.length * 0.5f, 0.6f));
                        if (!character.TryApply(selections[i], out error)) throw new InvalidOperationException(error);
                        path = Path.Combine(outputDir, "female-" + (i + 1).ToString("00") + "-relax.png"); WriteCamera(camera, target, path); report.images.Add(path);
                    }
                }
                report.passed = true;
            }
            catch (Exception ex) { failure = ex; report.failure = ex.ToString(); }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene); if (target) { target.Release(); Object.DestroyImmediate(target); }
                File.WriteAllText(Path.Combine(outputDir, "customization-renders.json"), JsonUtility.ToJson(report, true));
            }
            if (failure != null) throw new InvalidOperationException("Female preview failed; see customization-renders.json.", failure);
            Debug.Log("Rendered " + report.images.Count + " isolated female previews to " + outputDir);
        }
        private static void Light(Scene scene, string name, Vector3 angles, float intensity, Color color)
        {
            var go = new GameObject("Preview " + name); SceneManager.MoveGameObjectToScene(go, scene); go.transform.rotation = Quaternion.Euler(angles);
            var light = go.AddComponent<UnityEngine.Light>(); light.type = LightType.Directional; light.intensity = intensity; light.color = color; light.shadows = LightShadows.None;
        }
        private static AnimationClip RelaxClip()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { FemaleCustomizationSetup.AssetRoot }))
                foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)).OfType<AnimationClip>())
                    if (!clip.name.StartsWith("__preview__", StringComparison.Ordinal) && clip.name.IndexOf("relax", StringComparison.OrdinalIgnoreCase) >= 0) return clip;
            return null;
        }
        private static void WriteCamera(Camera camera, RenderTexture target, string path)
        {
            var old = RenderTexture.active; Texture2D texture = null;
            try
            {
                camera.Render(); RenderTexture.active = target; texture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); texture.Apply();
                var pixels = texture.GetPixels32(); var bg = pixels[0]; int count = 0;
                foreach (var p in pixels) if (Math.Abs(p.r - bg.r) + Math.Abs(p.g - bg.g) + Math.Abs(p.b - bg.b) > 20) count++;
                if (count < pixels.Length / 100) throw new InvalidOperationException("Original-material preview appears empty: " + path);
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally { RenderTexture.active = old; if (texture) Object.DestroyImmediate(texture); }
        }
    }
}
