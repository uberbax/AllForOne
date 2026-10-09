using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Animpic.CharacterStudio.Editor
{
    public static class CharacterStudioValidation
    {
        private const string Root = "Assets/Animpic Studio/POLY-FantasyCharacter";
        private static string Output { get { return Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/CharacterStudioValidation")); } }
        [Serializable] private sealed class Report
        {
            public bool passed; public string completedUtc, failure, lastCase, lifecycleDiagnostics;
            public int assertions, applications, models, lowerBodyCases, armCases, independentGloves, appearanceCases, paintedParts, hiddenPaintRestores, jsonRoundTrips, invalidInputs, invalidConfigurations, bakedMeshes;
        }
        private sealed class Pose
        {
            public Transform t, parent; public Vector3 position, scale; public Quaternion rotation; public bool active;
        }
        private sealed class Visual
        {
            public SkinnedMeshRenderer r; public Mesh mesh; public Material[] materials; public Transform[] bones; public Transform root;
            public bool enabled; public Color color; public float sentinel;
        }
        private sealed class Fixture : IDisposable
        {
            public readonly GameObject root; public readonly CharacterCustomizer c; public readonly CharacterCatalog catalog;
            public readonly Dictionary<string, SkinnedMeshRenderer> renderers; public readonly Dictionary<string, Appearance> visibleCases = new Dictionary<string, Appearance>();
            public readonly bool male; private readonly Report report; private readonly Pose[] poses; private readonly Visual[] rig;
            private readonly Dictionary<Material, string> materialAssets; private readonly int materialCount; private int changes;
            public Fixture(bool isMale, Report result)
            {
                male = isMale; report = result;
                string path = "Assets/Animpic Studio/POLY-FantasyCharacter/Prefabs/" + (male ? "POLY_FantasyMale_Studio" : "POLY_FantasyFemale_Studio") + ".prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Check(prefab != null, "Missing generated studio prefab: " + path);
                root = Object.Instantiate(prefab); root.name = "__CharacterStudioValidation_" + (male ? "Male" : "Female"); root.hideFlags = HideFlags.None;
                try
                {
                var animator = root.GetComponent<Animator>(); if (animator) animator.enabled = false;
                c = root.GetComponent<CharacterCustomizer>(); Check(c != null, "Missing shared customizer."); catalog = c.Catalog;
                Check(catalog != null && catalog.id == (male ? "poly-fantasy-male" : "poly-fantasy-female"), "Wrong catalog identity.");
                Check(catalog.parts.Length == (male ? 66 : 60), "Unexpected complete renderer inventory.");
                Check(catalog.torsos.Length == 4 && catalog.pants.Length == 4 && catalog.footwear.Length == 6 && catalog.gloves.Length == 4 && catalog.brows.Length == 3 && catalog.beards.Length == (male ? 3 : 0) && catalog.hair.Length == (male ? 6 : 3), "Option inventory differs from audit.");
                renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).ToDictionary(r => r.name, StringComparer.Ordinal);
                Check(renderers.Count == catalog.parts.Length, "Unowned or missing mesh renderers.");
                foreach (var renderer in renderers.Values)
                {
                    var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block); block.SetFloat("_StudioValidationSentinel", 17.25f); renderer.SetPropertyBlock(block);
                }
                poses = root.GetComponentsInChildren<Transform>(true).Select(t => new Pose { t = t, parent = t.parent, position = t.localPosition, rotation = t.localRotation, scale = t.localScale, active = t.gameObject.activeSelf }).ToArray();
                rig = Snapshot();
                materialAssets = catalog.palettes.Distinct().ToDictionary(m => m, m => EditorJsonUtility.ToJson(m));
                foreach (string style in new[] { "A", "B", "C", "D", "E", "F" })
                { var source = AssetDatabase.LoadAssetAtPath<Material>("Assets/Animpic Studio/POLY-FantasyCharacter/Materials/M_Main_" + style + ".mat"); if (source && !materialAssets.ContainsKey(source)) materialAssets.Add(source, EditorJsonUtility.ToJson(source)); }
                materialCount = Resources.FindObjectsOfTypeAll<Material>().Length;
                c.Changed += OnChanged;
                } catch { Object.DestroyImmediate(root); throw; }
            }
            private void OnChanged() { changes++; }
            public void Check(bool ok, string message) { report.assertions++; if (!ok) throw new InvalidOperationException(report.lastCase + ": " + message); }
            public void Apply(Appearance a, string name)
            {
                report.lastCase = (male ? "male / " : "female / ") + name; int before = changes;
                Check(c.TryApply(a, out var error), "TryApply failed: " + error); report.applications++;
                Check(changes == before + 1 && c.LastValidationError == null, "Apply did not signal exactly one successful change.");
                Verify(a);
                foreach (string id in Expected(male, a)) if (!visibleCases.ContainsKey(id)) visibleCases.Add(id, a.Copy());
            }
            public void Verify(Appearance a)
            {
                var expected = Expected(male, a);
                Check(expected.SetEquals(renderers.Values.Where(r => r.enabled).Select(r => r.name)), "Enabled parts differ from audited coverage rules.");
                var visible = c.GetVisibleParts(); Check(expected.SetEquals(visible.Select(p => p.id)), "Visible-part UI inventory is incorrect.");
                Check(visible.All(p => p.renderer == renderers[p.id] && !string.IsNullOrWhiteSpace(p.label) && !p.label.StartsWith("Cube_", StringComparison.Ordinal)), "Visible elements lack readable labels or have wrong renderer handles.");
                foreach (var p in catalog.parts)
                {
                    var r = renderers[p.id]; var paint = a.paints.FirstOrDefault(x => x.partId == p.id);
                    int palette = paint == null || paint.palette < 0 ? a.basePalette : paint.palette;
                    Check(r.sharedMaterials.Length == r.sharedMesh.subMeshCount && r.sharedMaterials.All(m => m == catalog.palettes[palette]), "Palette leaked or did not apply to " + p.id);
                    var block = new MaterialPropertyBlock(); r.GetPropertyBlock(block);
                    Check(Near(block.GetColor("_Color"), paint == null ? Color.white : paint.tint), "Tint leaked or did not apply to " + p.id);
                    Check(Mathf.Abs(block.GetFloat("_StudioValidationSentinel") - 17.25f) < 0.0001f, "Unrelated property-block data was erased.");
                    Mesh mesh = p.mesh;
                    if (a.leftGlove == 2 && p.id == (male ? "Cube_026" : "CHR_Hand_L")) mesh = catalog.gloves[1].leftCompanionMesh;
                    if (a.rightGlove == 2 && p.id == (male ? "Cube_074" : "CHR_Hand_R")) mesh = catalog.gloves[1].rightCompanionMesh;
                    Check(r.sharedMesh == mesh, "Wrong original or glove companion mesh: " + p.id);
                }
                Hierarchy();
            }
            public void Hierarchy()
            {
                Check(root.GetComponentsInChildren<Transform>(true).Length == poses.Length, "Hierarchy size changed.");
                foreach (var p in poses) Check(p.t && p.t.parent == p.parent && p.t.localPosition == p.position && p.t.localRotation == p.rotation && p.t.localScale == p.scale && p.t.gameObject.activeSelf == p.active, "A transform, bone pose or GameObject activation changed.");
                foreach (var r in rig) Check(r.r.rootBone == r.root && r.r.bones.SequenceEqual(r.bones), "Skeleton was rebound.");
            }
            public Visual[] Snapshot()
            {
                return renderers.Values.Select(r => { var b = new MaterialPropertyBlock(); r.GetPropertyBlock(b); return new Visual { r = r, mesh = r.sharedMesh, materials = r.sharedMaterials, enabled = r.enabled, color = b.GetColor("_Color"), sentinel = b.GetFloat("_StudioValidationSentinel"), bones = r.bones, root = r.rootBone }; }).ToArray();
            }
            public void Unchanged(Visual[] before, string json, int eventCount)
            {
                Check(c.SaveJson() == json && changes == eventCount, "Invalid input changed appearance or fired Changed.");
                foreach (var v in before)
                {
                    var b = new MaterialPropertyBlock(); v.r.GetPropertyBlock(b);
                    Check(v.r.enabled == v.enabled && v.r.sharedMesh == v.mesh && v.r.sharedMaterials.SequenceEqual(v.materials) && Near(b.GetColor("_Color"), v.color) && b.GetFloat("_StudioValidationSentinel") == v.sentinel, "Rejected input partially changed a renderer.");
                }
                Hierarchy();
            }
            public void Reject(Appearance bad)
            {
                report.lastCase = (male ? "male" : "female") + " / invalid appearance atomicity";
                var before = Snapshot(); string json = c.SaveJson(); int events = changes;
                Check(!c.TryApply(bad, out var error) && !string.IsNullOrWhiteSpace(error), "Invalid appearance accepted or no diagnostic.");
                Unchanged(before, json, events); report.invalidInputs++;
            }
            public void RejectJson(string bad)
            {
                report.lastCase = (male ? "male" : "female") + " / invalid JSON atomicity";
                var before = Snapshot(); string json = c.SaveJson(); int events = changes;
                Check(!c.TryLoadJson(bad, out var error) && !string.IsNullOrWhiteSpace(error), "Invalid JSON accepted."); Unchanged(before, json, events); report.invalidInputs++;
            }
            public void RejectCatalog(Action<CharacterCatalog> mutate)
            {
                report.lastCase = (male ? "male" : "female") + " / invalid catalog atomicity";
                var clone = Object.Instantiate(catalog); var before = Snapshot(); string json = c.SaveJson(); int events = changes;
                try { mutate(clone); c.Configure(clone); Check(!c.TryApply(c.Capture(), out var error) && !string.IsNullOrWhiteSpace(error), "Invalid catalog accepted."); Unchanged(before, json, events); report.invalidConfigurations++; }
                finally { c.Configure(catalog); Object.DestroyImmediate(clone); }
            }
            public void RoundTrip()
            {
                string json = c.SaveJson(); var expected = c.Capture(); int before = changes;
                Check(c.TryLoadJson(json, out var error), "JSON round trip failed: " + error); Check(c.SaveJson() == json && changes == before + 1, "JSON round trip changed appearance.");
                Verify(expected); report.jsonRoundTrips++; report.applications++;
            }
            public void RestoreAfterEnable()
            {
                report.lastCase = (male ? "male" : "female") + " / restore nonserialized tint after enable";
                var expected = c.Capture(); int before = changes;
                c.enabled = false;
                foreach (var renderer in renderers.Values)
                { var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block); block.SetColor("_Color", Color.black); renderer.SetPropertyBlock(block); }
                c.enabled = true;
                report.lifecycleDiagnostics = "changedDelta=" + (changes - before) + "; sceneValid=" + root.scene.IsValid() +
                    "; sceneLoaded=" + root.scene.isLoaded + "; scenePath=" + root.scene.path +
                    "; activeInHierarchy=" + root.activeInHierarchy + "; componentEnabled=" + c.enabled +
                    "; activeAndEnabled=" + c.isActiveAndEnabled + "; runInEditMode=" + c.runInEditMode +
                    "; objectHideFlags=" + root.hideFlags + "; componentHideFlags=" + c.hideFlags +
                    "; isPlaying=" + Application.isPlaying + "; lastValidationError=" + c.LastValidationError;
                Check(changes == before + 1, "OnEnable did not restore appearance exactly once. " + report.lifecycleDiagnostics);
                Verify(expected); report.applications++;
            }
            public void Finish()
            {
                foreach (var m in materialAssets) Check(EditorJsonUtility.ToJson(m.Key) == m.Value, "A palette/source Material asset was mutated.");
                Check(Resources.FindObjectsOfTypeAll<Material>().Length == materialCount, "Customization instantiated or leaked a Material.");
            }
            public void Dispose() { if (c) c.Changed -= OnChanged; if (root) Object.DestroyImmediate(root); }
        }

        [MenuItem("Tools/Animpic Studio/Characters/Fantasy Character/Validation/Character Studio in Editor", false, 300)]
        public static void ValidateAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Run the complete audit from Edit mode.");
            var report = new Report(); Directory.CreateDirectory(Output);
            try { ValidateModel(false, report); ValidateModel(true, report); report.passed = true; Debug.Log("Character Studio validation passed: " + report.applications + " appearances, " + report.assertions + " assertions."); }
            catch (Exception e) { report.failure = e.ToString(); throw; }
            finally { report.completedUtc = DateTime.UtcNow.ToString("O"); File.WriteAllText(Path.Combine(Output, "validation.json"), JsonUtility.ToJson(report, true)); }
        }
        public static void ValidatePlayMode() { CharacterStudioPlayValidation.Start(); }
        private static Appearance Base(Fixture f) { return new Appearance { catalogId = f.catalog.id }; }
        private static void ValidateModel(bool male, Report report)
        {
            report.lastCase = (male ? "male" : "female") + " / prefab inventory";
            using (var f = new Fixture(male, report))
            {
                for (int torso = 0; torso < 4; torso++) for (int pants = 0; pants < 4; pants++) for (int shoes = 0; shoes <= 6; shoes++)
                { var a = Base(f); a.torso = torso; a.pants = pants; a.footwear = shoes; f.Apply(a, "lower body " + torso + "/" + pants + "/" + shoes); report.lowerBodyCases++; }
                for (int torso = 0; torso < 4; torso++) for (int side = 0; side < 2; side++) for (int mode = 0; mode < 2; mode++) for (int style = -1; style < 4; style++) for (int glove = 0; glove <= 4; glove++)
                {
                    var a = Base(f); a.torso = torso;
                    if (side == 0) { a.leftArmMode = (ArmMode)mode; a.leftForearmStyle = style; a.leftGlove = glove; }
                    else { a.rightArmMode = (ArmMode)mode; a.rightForearmStyle = style; a.rightGlove = glove; }
                    f.Apply(a, "arm " + torso + "/" + side + "/" + mode + "/" + style + "/" + glove); report.armCases++;
                    if (glove > 0) { if (side == 0) a.leftGlove = 0; else a.rightGlove = 0; f.Apply(a, "remove glove and restore saved arm"); }
                }
                for (int torso = 0; torso < 4; torso++) for (int left = 0; left <= 4; left++) for (int right = 0; right <= 4; right++)
                { var a = Base(f); a.torso = torso; a.leftArmMode = a.rightArmMode = ArmMode.Split; a.leftForearmStyle = 2; a.rightForearmStyle = 3; a.leftGlove = left; a.rightGlove = right; f.Apply(a, "independent gloves " + torso + "/" + left + "/" + right); report.independentGloves++; }
                for (int hair = 0; hair <= (male ? 6 : 3); hair++) for (int brows = 0; brows <= 3; brows++) for (int beard = 0; beard <= (male ? 3 : 0); beard++)
                { var a = Base(f); a.hair = hair; a.brows = brows; a.beard = beard; f.Apply(a, "hair/brows/beard " + hair + "/" + brows + "/" + beard); report.appearanceCases++; }
                f.Check(f.visibleCases.Count == f.catalog.parts.Length, "Some imported/generated elements were never selected by the audited option cases.");
                foreach (var part in f.catalog.parts)
                {
                    var a = f.visibleCases[part.id].Copy(); a.basePalette = 0; a.paints = new[] { new PartPaint { partId = part.id, palette = 5, tint = new Color(0.25f, 0.65f, 0.85f, 1) } };
                    f.Apply(a, "isolated paint " + part.id); f.RoundTrip(); report.paintedParts++;
                    var hidden = f.visibleCases.Values.FirstOrDefault(x => !Expected(male, x).Contains(part.id));
                    if (hidden != null) { var hide = hidden.Copy(); hide.paints = a.Copy().paints; f.Apply(hide, "retain hidden paint " + part.id); f.Apply(a, "restore painted part " + part.id); report.hiddenPaintRestores++; }
                    a.paints[0].palette = -1; a.basePalette = 3; f.Apply(a, "inherit base palette with independent tint " + part.id);
                }
                var neutral = Base(f); f.Apply(neutral, "reset before input rejection");
                f.Reject(null);
                Action<Appearance>[] invalid = { a => a.catalogId = "wrong-character", a => a.torso = -1, a => a.pants = 4, a => a.footwear = 7, a => a.leftArmMode = (ArmMode)17, a => a.rightForearmStyle = 4, a => a.beard = male ? 4 : 1, a => a.basePalette = 6, a => a.paints = null, a => a.extras = null,
                    a => a.paints = new[] { new PartPaint { partId = "unknown" } }, a => a.paints = new[] { new PartPaint { partId = f.catalog.parts[0].id, tint = new Color(float.NaN, 1, 1, 1) } },
                    a => a.paints = new[] { new PartPaint { partId = f.catalog.parts[0].id }, new PartPaint { partId = f.catalog.parts[0].id } } };
                foreach (var change in invalid) { var a = Base(f); change(a); f.Reject(a); }
                foreach (string bad in new[] { "", "{}", "null", "[]", "{\"schemaVersion\":1,\"appearance\":null}", "{\"schemaVersion\":1,\"appearance\":{}}", "{\"schemaVersion\":2,\"appearance\":{}}", f.c.SaveJson() + " trailing" }) f.RejectJson(bad);
                f.RejectCatalog(c => c.parts[0].id = c.parts[1].id);
                f.RejectCatalog(c => c.parts[0].requiredBoneNames[0] = "invalid-bone");
                f.RejectCatalog(c => c.gloves[1].leftCompanionMesh = null);
                f.RejectCatalog(c => c.tintProperties = new[] { "_MainTex" });
                f.RejectCatalog(c => c.torsos[0].parts = new[] { "missing-renderer" });
                f.RejectCatalog(c => c.parts[0].mesh = null);
                report.lastCase = (male ? "male" : "female") + " / determinism and caller ownership";
                var randomState = UnityEngine.Random.state;
                f.Check(f.c.Randomize(1046, out var error), "Randomization failed: " + error); string first = f.c.SaveJson();
                f.Check(f.c.Randomize(1046, out error) && first == f.c.SaveJson(), "Randomization is not deterministic.");
                f.Check(UnityEngine.Random.state.Equals(randomState), "Global Unity random state changed."); f.Verify(f.c.Capture());
                var copy = f.c.Capture(); copy.basePalette = (copy.basePalette + 1) % 6; f.Check(first == f.c.SaveJson(), "Capture exposed live selection data.");
                if (male)
                {
                    f.Check(Triangles(f.renderers["Generated_D_Forearm_R"].sharedMesh) == 144, "Generated male right D forearm must contain 144 triangles.");
                    f.Check(Triangles(f.renderers["Cube_129"].sharedMesh) == 148, "Clean male left D forearm must contain 148 triangles.");
                }
                var split = Base(f); split.torso = 3; split.leftArmMode = split.rightArmMode = ArmMode.Split; f.Apply(split, "bake both D forearms");
                foreach (var r in f.renderers.Values.Where(r => r.enabled)) { Bake(f, r); report.bakedMeshes++; }
                f.RestoreAfterEnable(); f.Finish(); report.models++;
            }
        }
        private static long Triangles(Mesh mesh) { long indices = 0; for (int sub = 0; sub < mesh.subMeshCount; sub++) indices += mesh.GetIndexCount(sub); return indices / 3; }
        private static void Bake(Fixture f, SkinnedMeshRenderer renderer)
        {
            var mesh = new Mesh();
            try { renderer.BakeMesh(mesh); f.Check(mesh.vertexCount > 0, "Empty baked mesh: " + renderer.name); foreach (var v in mesh.vertices) f.Check(Finite(v.x) && Finite(v.y) && Finite(v.z), "Nonfinite skinned geometry: " + renderer.name); }
            finally { Object.DestroyImmediate(mesh); }
        }
        private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        private static bool Near(Color a, Color b) { return Mathf.Abs(a.r - b.r) < 0.00001f && Mathf.Abs(a.g - b.g) < 0.00001f && Mathf.Abs(a.b - b.b) < 0.00001f && Mathf.Abs(a.a - b.a) < 0.00001f; }
        private static string Style(int index) { return ((char)('A' + index)).ToString(); }
        private static readonly string[] MaleTorsos = { "Cube_010", "Cube_078", "Cube_082", "Cube_121" };
        private static readonly string[,] MalePants = { { "Cube_015", "Cube_003" }, { "Cube_063", "Cube_004" }, { "Cube_133", "Cube_132" }, { "Cube_118", "Cube_202" } };
        private static readonly string[] MaleShoes = { "Cube_011", "Cube_020", "Cube_182", "Cube_116", "Cube_193", "Cube_195" };
        private static readonly string[] MaleHair = { "Cube_008", "Cube_170", "Cube_012", "Cube_168", "Cube_090", "Cube_169" };
        private static readonly string[] MaleBrows = { "Cube_207", "Cube_229", "Cube_232" }, MaleBeards = { "Cube_023", "Cube_087", "Cube_092" };
        private static readonly string[,] MaleArms = { { "Cube_002", "Cube_016", "Cube_134", "Cube_006", "Cube_018", "Cube_137" }, { "Cube_095", "Cube_097", "Cube_140", "Cube_094", "Cube_096", "Cube_141" }, { "Cube_088", "Cube_100", "Cube_112", "Cube_099", "Cube_111", "Cube_113" }, { "Cube_123", "Cube_130", "Cube_129", "Cube_145", "Cube_156", "Generated_D_Forearm_R" } };
        private static readonly string[,] MaleGloves = { { "Cube_017", "Cube_013" }, { "Cube_103", "Cube_104" }, { "Cube_186", "Cube_189" }, { "Cube_175", "Cube_176" } };
        private static HashSet<string> Expected(bool male, Appearance a)
        {
            var result = new HashSet<string>(StringComparer.Ordinal) { male ? "Cube_144" : "CHR_Female_Head", male ? MaleTorsos[a.torso] : a.torso == 0 ? "CHR_Frmale_Torso_A" : "CHR_Female_Torso_" + Style(a.torso) };
            bool high = a.footwear >= 1 && a.footwear <= 4;
            result.Add(male ? MalePants[a.pants, high ? 1 : 0] : "CHR_Female_Pant_" + Style(a.pants) + (high ? "_02" : "_01"));
            result.Add(a.footwear == 0 ? (male ? "Cube_135" : "CHR_Legs") : male ? MaleShoes[a.footwear - 1] : "SM_Boots_" + Style(a.footwear - 1));
            if (a.hair > 0) result.Add(male ? MaleHair[a.hair - 1] : "SM_Hair_" + Style(a.hair + 2) + "_01");
            if (a.brows > 0) result.Add(male ? MaleBrows[a.brows - 1] : "SM_Female_Brows_" + Style(a.brows - 1));
            if (a.beard > 0) result.Add(MaleBeards[a.beard - 1]);
            ExpectedArm(male, a, true, result); ExpectedArm(male, a, false, result); return result;
        }
        private static void ExpectedArm(bool male, Appearance a, bool left, HashSet<string> result)
        {
            int offset = left ? 0 : 3; string side = left ? "L" : "R", prefix = "CHR_Torso_" + Style(a.torso) + "_Hand_" + side;
            int glove = left ? a.leftGlove : a.rightGlove;
            if (glove > 0)
            {
                result.Add(male ? MaleArms[a.torso, offset + 1] : prefix + "_02"); result.Add(male ? MaleGloves[glove - 1, left ? 0 : 1] : "CHR_Glove_" + Style(glove - 1) + "_" + side);
                if (glove == 2) result.Add(male ? left ? "Cube_026" : "Cube_074" : "CHR_Hand_" + side); return;
            }
            if ((left ? a.leftArmMode : a.rightArmMode) == ArmMode.Full) { result.Add(male ? MaleArms[a.torso, offset] : prefix + "_01"); return; }
            int lower = left ? a.leftForearmStyle : a.rightForearmStyle; if (lower < 0) lower = a.torso;
            result.Add(male ? MaleArms[a.torso, offset + 1] : prefix + "_02"); result.Add(male ? MaleArms[lower, offset + 2] : "CHR_Torso_" + Style(lower) + "_Hand_" + side + "_03");
        }
    }
}