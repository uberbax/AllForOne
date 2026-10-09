using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Animpic.CharacterStudio.Editor
{
    public static class CharacterStudioUIValidation
    {
        [Serializable] private sealed class Report
        {
            public string mode, failure, completedUtc;
            public bool passed, restored, profilesRestored;
            public int assertions, buttons, sliders, layoutChecks, characters, screenWidth, screenHeight;
            public int referenceWidth = 1920, referenceHeight = 1080;
            public List<string> errors = new List<string>();
        }
        private sealed class Profile { public string key, value; public bool exists; }
        private sealed class Surface { public SkinnedMeshRenderer r; public Material[] materials; public Color[] tint; }
        private sealed class Test
        {
            public CharacterStudioUI ui; public Report report; public string step;
            public void Check(bool ok, string why) { report.assertions++; if (!ok) throw new InvalidOperationException(step + ": " + why); }
            public Transform Row(string name, int index = 0)
            {
                var rows = ui.uiCanvas.GetComponentsInChildren<Transform>(false).Where(t => t.name == name).ToArray();
                Check(rows.Length > index, "Missing control: " + name + " #" + index); return rows[index];
            }
            public void Click(string name) { Click(ui.uiCanvas.GetComponentsInChildren<Button>(false).Single(b => b.name == name)); }
            public void Click(Button button)
            {
                Check(button && button.interactable && button.gameObject.activeInHierarchy, "Unavailable button");
                button.onClick.Invoke(); report.buttons++; Layout();
                Check(report.errors.Count == 0, "UI emitted errors: " + string.Join(" | ", report.errors));
            }
            public void Arrow(string name, int index = 0) { Click(Row(name + " controls", index).GetComponentsInChildren<Button>(false).Single(b => b.name == "›")); }
            public void Swatch(int group, int value) { Click(Row("Palette swatches", group).GetComponentsInChildren<Button>(false).Single(b => b.name == ((char)('A' + value)).ToString())); }
            public void Layout() { Canvas.ForceUpdateCanvases(); LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)ui.uiCanvas.transform); Canvas.ForceUpdateCanvases(); }
        }
        public static void ValidateEditUI() { Run(false); }
        public static void ValidateRuntimeUI() { Run(true); }
        private static void Run(bool play)
        {
            if (Application.isPlaying != play) throw new InvalidOperationException(play ? "Play mode required." : "Edit mode required.");
            var scene = SceneManager.GetActiveScene();
            if (scene.name != "CharacterStudio") throw new InvalidOperationException("The existing CharacterStudio scene must be active.");
            var interfaces = Object.FindObjectsOfType<CharacterStudioUI>(true).Where(u => u.gameObject.scene == scene).ToArray();
            if (interfaces.Length != 1) throw new InvalidOperationException("Expected one CharacterStudioUI.");
            var ui = interfaces[0];
            if (!ui.female || !ui.male) throw new InvalidOperationException("Both character references are required.");
            var report = new Report { mode = play ? "Play" : "Edit", screenWidth = Screen.width, screenHeight = Screen.height };
            var t = new Test { ui = ui, report = report, step = "Setup" };
            var female = ui.female.Capture(); var male = ui.male.Capture();
            bool femaleActive = ui.female.gameObject.activeSelf, maleActive = ui.male.gameObject.activeSelf, dirty = scene.isDirty;
            bool originalMale = Read<CharacterCustomizer>(ui, "current") == ui.male || (Read<CharacterCustomizer>(ui, "current") == null && maleActive);
            int originalTab = Read<int>(ui, "tab"); string originalPart = Read<string>(ui, "paintedPart");
            var oldStatus = Read<Text>(ui, "status"); string statusText = oldStatus ? oldStatus.text : "Changes are shown instantly.";
            bool rotate = ui.presentation && ui.presentation.autoRotate;
            var profiles = new[] { ui.female, ui.male }.Select(c => {
                string key = "Animpic.CharacterStudio.Profile." + c.Catalog.id;
                return new Profile { key = key, exists = PlayerPrefs.HasKey(key), value = PlayerPrefs.GetString(key, "") };
            }).ToArray();
            var materialState = new Dictionary<Material, Color[]>();
            foreach (var c in new[] { ui.female, ui.male }) foreach (var m in c.Catalog.palettes)
                if (!materialState.ContainsKey(m)) materialState.Add(m, c.Catalog.tintProperties.Select(p => m.HasProperty(p) ? m.GetColor(p) : Color.clear).ToArray());
            Exception failure = null;
            Application.LogCallback log = (message, trace, kind) => { if (kind == LogType.Error || kind == LogType.Exception || kind == LogType.Assert) report.errors.Add(message); };
            Application.logMessageReceived += log;
            try
            {
                // Rebind nonserialized edit-time button closures without opening or rebuilding the scene.
                if (!play) ui.BuildUI();
                Structure(t, scene);
                foreach (var c in new[] { ui.female, ui.male })
                {
                    t.step = c.Catalog.label + " gender / reset";
                    t.Click(c == ui.male ? "Male" : "Female");
                    t.Check(Read<CharacterCustomizer>(ui, "current") == c, "Gender callback failed.");
                    t.Check(c.gameObject.activeSelf && !(c == ui.female ? ui.male : ui.female).gameObject.activeSelf, "Gender activation is not exclusive.");
                    var other = c == ui.female ? ui.male : ui.female; string otherBefore = other.SaveJson();
                    t.Click("Reset look");
                    var baseline = Read<Appearance>(ui, c == ui.male ? "maleDefault" : "femaleDefault") ?? new Appearance { catalogId = c.Catalog.id };
                    t.Check(Json(c.Capture()) == Json(baseline), "Reset callback failed. Apply error: " + c.LastValidationError + " Expected: " + Json(baseline) + " Actual: " + Json(c.Capture()));
                    AppearanceControls(t, c); OutfitControls(t, c); ColourControls(t, c);
                    t.step = c.Catalog.label + " save/load";
                    string saved = c.SaveJson(); t.Click("Save look"); t.Click("Outfit"); t.Arrow("Torso");
                    t.Check(c.SaveJson() != saved, "Profile test did not change state."); t.Click("Load look"); t.Check(c.SaveJson() == saved, "Profile load did not restore saved look.");
                    t.Click("Randomize"); t.Check(c.ValidateConfiguration(out var error), "Randomize invalid: " + error);
                    t.Click("Reset look"); t.Check(Json(c.Capture()) == Json(baseline), "Reset after randomize failed.");
                    t.Check(other.SaveJson() == otherBefore, "Editing a character changed the other character.");
                    report.characters++;
                }
                if (ui.presentation) { bool before = ui.presentation.autoRotate; t.Click("Auto rotate"); t.Check(ui.presentation.autoRotate != before, "Auto rotation did not toggle."); t.Click("Auto rotate"); t.Check(ui.presentation.autoRotate == before, "Auto rotation did not toggle back."); }
                foreach (var c in new[] { ui.female, ui.male }) foreach (var m in c.Catalog.palettes)
                    t.Check(materialState[m].SequenceEqual(c.Catalog.tintProperties.Select(p => m.HasProperty(p) ? m.GetColor(p) : Color.clear)), "Shared material asset was changed.");
                Structure(t, scene); report.passed = true;
            }
            catch (Exception ex) { failure = ex; report.failure = ex.ToString(); }
            finally
            {
                // Restore even when a callback or assertion fails. Existing user profile values are retained exactly.
                try
                {
                    string error;
                    if (!ui.female.TryApply(female, out error) || !ui.male.TryApply(male, out error)) throw new InvalidOperationException("Restore appearance: " + error);
                    ui.SetCharacter(originalMale);
                    var tabButton = ui.uiCanvas.GetComponentsInChildren<Button>(false).Single(b => b.name == new[] { "Appearance", "Outfit", "Colour" }[Mathf.Clamp(originalTab, 0, 2)]);
                    tabButton.onClick.Invoke();
                    if (originalPart != null) { Write(ui, "paintedPart", originalPart); typeof(CharacterStudioUI).GetMethod("RefreshContent", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(ui, null); }
                    ui.female.gameObject.SetActive(femaleActive); ui.male.gameObject.SetActive(maleActive);
                    if (ui.presentation) ui.presentation.SetAutoRotate(rotate);
                    var text = Read<Text>(ui, "status"); if (text) text.text = statusText;
                    report.restored = Json(ui.female.Capture()) == Json(female) && Json(ui.male.Capture()) == Json(male) && ui.female.gameObject.activeSelf == femaleActive && ui.male.gameObject.activeSelf == maleActive;
                    if (!report.restored) throw new InvalidOperationException("Original character state was not restored.");

                }
                catch (Exception ex) { report.passed = false; report.failure += "\nRESTORE: " + ex; if (failure == null) failure = ex; }
                try
                {
                    foreach (var p in profiles) { if (p.exists) PlayerPrefs.SetString(p.key, p.value); else PlayerPrefs.DeleteKey(p.key); }
                    PlayerPrefs.Save();
                    report.profilesRestored = profiles.All(p => PlayerPrefs.HasKey(p.key) == p.exists && (!p.exists || PlayerPrefs.GetString(p.key) == p.value));
                    if (!report.profilesRestored) throw new InvalidOperationException("Local profiles were not restored.");
                }
                catch (Exception ex) { report.passed = false; report.failure += "\nPROFILE RESTORE: " + ex; if (failure == null) failure = ex; }
                Application.logMessageReceived -= log; report.completedUtc = DateTime.UtcNow.ToString("O");
                string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/CharacterStudioValidation")); Directory.CreateDirectory(output);
                string name = play ? "ui-play" : "ui-edit";
                File.WriteAllText(Path.Combine(output, name + ".json"), JsonUtility.ToJson(report, true));
                File.WriteAllText(Path.Combine(output, name + ".txt"), (report.passed ? "PASS" : "FAIL") + " actual UGUI callbacks\n" + JsonUtility.ToJson(report, true));
            }
            if (failure != null) throw new InvalidOperationException("UI validation failed; see Library/CharacterStudioValidation/ui-" + (play ? "play" : "edit") + ".json", failure);
            Debug.Log("Character Studio " + report.mode + " UI passed: " + report.assertions + " assertions, " + report.buttons + " button callbacks, " + report.sliders + " slider callbacks.");
        }


        private static void AppearanceControls(Test t, CharacterCustomizer c)
        {
            t.step = c.Catalog.label + " appearance"; t.Click("Appearance"); t.Check(Read<int>(t.ui, "tab") == 0, "Appearance tab failed.");
            int before = c.Capture().hair; t.Arrow("Hairstyle"); t.Check(c.Capture().hair == (before + 1) % (c.Catalog.hair.Length + 1), "Hair arrow failed.");
            before = c.Capture().brows; t.Arrow("Eyebrows"); t.Check(c.Capture().brows == (before + 1) % (c.Catalog.brows.Length + 1), "Brow arrow failed.");
            if (c.Catalog.beards.Length > 0) { before = c.Capture().beard; t.Arrow("Facial hair"); t.Check(c.Capture().beard == (before + 1) % (c.Catalog.beards.Length + 1), "Beard arrow failed."); }
            foreach (var slot in c.Catalog.extras)
            {
                before = c.Capture().extras.Where(e => e.id == slot.id).Select(e => e.option).DefaultIfEmpty(0).First();
                t.Arrow(slot.label);
                t.Check(c.Capture().extras.Where(e => e.id == slot.id).Select(e => e.option).DefaultIfEmpty(0).First() == (before + 1) % (slot.options.Length + 1), "Extra slot arrow failed.");
            }
            LayoutChecks(t);
        }
        private static void OutfitControls(Test t, CharacterCustomizer c)
        {
            t.step = c.Catalog.label + " outfit"; t.Click("Outfit"); t.Check(Read<int>(t.ui, "tab") == 1, "Outfit tab failed.");
            int before = c.Capture().torso; t.Arrow("Torso"); t.Check(c.Capture().torso == (before + 1) % c.Catalog.torsos.Length, "Torso arrow failed.");
            before = c.Capture().pants; t.Arrow("Trousers"); t.Check(c.Capture().pants == (before + 1) % c.Catalog.pants.Length, "Pants arrow failed.");
            for (int i = 0; i <= c.Catalog.footwear.Length; i++)
            {
                before = c.Capture().footwear; t.Arrow("Footwear"); var s = c.Capture();
                t.Check(s.footwear == (before + 1) % (c.Catalog.footwear.Length + 1), "Footwear arrow failed.");
                var visible = new HashSet<string>(c.GetVisibleParts().Select(p => p.id));
                foreach (string foot in c.Catalog.bareFeetParts) t.Check(visible.Contains(foot) == (s.footwear == 0), "Wrong feet visibility after footwear callback.");
                bool high = s.footwear > 0 && c.Catalog.footwear[s.footwear - 1].high;
                foreach (string part in high ? c.Catalog.pants[s.pants].shortParts : c.Catalog.pants[s.pants].longParts) t.Check(visible.Contains(part), "Wrong pants length after footwear callback.");
            }
            for (int side = 0; side < 2; side++)
            {
                bool left = side == 0; var saved = c.Capture(); t.Arrow("Construction", side); var changed = c.Capture();
                t.Check((int)(left ? changed.leftArmMode : changed.rightArmMode) == 1 - (int)(left ? saved.leftArmMode : saved.rightArmMode), "Arm mode failed.");
                t.Check((left ? changed.rightArmMode : changed.leftArmMode) == (left ? saved.rightArmMode : saved.leftArmMode), "Arm control affected opposite side.");
                before = left ? changed.leftForearmStyle : changed.rightForearmStyle; t.Arrow("Forearm style", side); var arm = c.Capture();
                t.Check((left ? arm.leftForearmStyle : arm.rightForearmStyle) == (before + 2) % (c.Catalog.torsos.Length + 1) - 1, "Forearm arrow failed.");
                for (int i = 0; i <= c.Catalog.gloves.Length; i++)
                {
                    before = left ? c.Capture().leftGlove : c.Capture().rightGlove; t.Arrow("Glove", side); var after = c.Capture();
                    t.Check((left ? after.leftGlove : after.rightGlove) == (before + 1) % (c.Catalog.gloves.Length + 1), "Glove arrow failed.");
                    t.Check((left ? after.leftArmMode : after.rightArmMode) == (left ? arm.leftArmMode : arm.rightArmMode) &&
                        (left ? after.leftForearmStyle : after.rightForearmStyle) == (left ? arm.leftForearmStyle : arm.rightForearmStyle), "Glove switching lost saved arm style.");
                }
            }
            LayoutChecks(t);
        }
        private static void ColourControls(Test t, CharacterCustomizer c)
        {
            t.step = c.Catalog.label + " colour"; t.Click("Colour"); t.Check(Read<int>(t.ui, "tab") == 2, "Colour tab failed.");
            int palette = (c.Capture().basePalette + 1) % 6; t.Swatch(0, palette); t.Check(c.Capture().basePalette == palette, "Global palette callback failed.");
            var visible = c.GetVisibleParts(); t.Check(visible.Length > 1, "Need multiple visible parts for colour isolation.");
            string original = c.SaveJson(); t.Arrow("Element"); t.Check(c.SaveJson() == original, "Selecting paint target changed the character.");
            string id = Read<string>(t.ui, "paintedPart"); var target = visible.Single(p => p.id == id);
            var others = visible.Where(p => p.id != id).Select(p => Capture(p.renderer, c.Catalog.tintProperties)).ToArray();
            int own = (palette + 2) % 6; t.Swatch(1, own);
            t.Check(c.Capture().paints.Single(p => p.partId == id).palette == own, "Individual palette callback failed.");
            t.Check(target.renderer.sharedMaterials.All(m => m == c.Catalog.palettes[own]), "Individual palette did not reach renderer.");
            string[] names = { "Red slider", "Green slider", "Blue slider" }; float[] values = { .31f, .58f, .83f };
            for (int i = 0; i < names.Length; i++)
            {
                var slider = t.ui.uiCanvas.GetComponentsInChildren<Slider>(false).Single(s => s.name == names[i]);
                slider.value = values[i]; t.report.sliders++; t.Layout();
                t.Check(Mathf.Abs(c.Capture().paints.Single(p => p.partId == id).tint[i] - values[i]) < .0001f, "Tint slider callback failed.");
            }
            var block = new MaterialPropertyBlock(); target.renderer.GetPropertyBlock(block); var tint = c.Capture().paints.Single(p => p.partId == id).tint;
            foreach (string p in c.Catalog.tintProperties) if (target.renderer.sharedMaterial.HasProperty(p)) t.Check(block.GetColor(p) == tint, "Tint missing from property block.");
            foreach (var other in others) t.Check(Same(other, c.Catalog.tintProperties), "Individual paint affected another renderer.");
            t.Click("Use character palette"); t.Check(c.Capture().paints.Single(p => p.partId == id).palette == -1, "Inherit button failed.");
            t.Click("Reset this element"); t.Check(c.Capture().paints.All(p => p.partId != id), "Element reset failed.");
            LayoutChecks(t);
        }
        private static Surface Capture(SkinnedMeshRenderer r, string[] properties)
        { var block = new MaterialPropertyBlock(); r.GetPropertyBlock(block); return new Surface { r = r, materials = r.sharedMaterials, tint = properties.Select(p => block.GetColor(p)).ToArray() }; }
        private static bool Same(Surface s, string[] properties)
        { var block = new MaterialPropertyBlock(); s.r.GetPropertyBlock(block); return s.materials.SequenceEqual(s.r.sharedMaterials) && s.tint.SequenceEqual(properties.Select(p => block.GetColor(p))); }
        private static void Structure(Test t, Scene scene)
        {
            t.step = "UI structure";
            t.Check(scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Canvas>(false)).Count() == 1, "Expected one active canvas.");
            t.Check(scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<EventSystem>(false)).Count() == 1, "Expected one active EventSystem.");
            t.Check(t.ui.uiCanvas.GetComponent<GraphicRaycaster>(), "Canvas pointer raycaster missing.");
            var scaler = t.ui.uiCanvas.GetComponent<CanvasScaler>();
            t.Check(scaler && scaler.referenceResolution == new Vector2(1920, 1080) && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize, "Wrong responsive reference canvas.");
            var scroll = t.ui.uiCanvas.GetComponentInChildren<ScrollRect>();
            t.Check(scroll && scroll.vertical && !scroll.horizontal && scroll.viewport && scroll.viewport.GetComponent<RectMask2D>(), "Settings scrolling/clipping missing.");
            t.Check(scroll.viewport.GetComponent<Image>().raycastTarget, "Settings whitespace cannot receive scroll events.");
            LayoutChecks(t);
        }
        private static void LayoutChecks(Test t)
        {
            t.Layout(); var canvas = (RectTransform)t.ui.uiCanvas.transform;
            t.Check(canvas.rect.width > 0 && canvas.rect.height > 0, "Canvas has no dimensions.");
            var panel = Relative((RectTransform)t.Row("Customization panel"), canvas);
            var identity = Relative((RectTransform)t.Row("Character identity"), canvas);
            t.Check(Contains(canvas.rect, panel), "Settings panel clips outside canvas."); t.Check(Contains(canvas.rect, identity), "Identity panel clips outside canvas.");
            t.Check(!panel.Overlaps(identity), "Identity and settings panels overlap.");
            var scroll = t.ui.uiCanvas.GetComponentInChildren<ScrollRect>();
            t.Check(scroll.content.rect.width <= scroll.viewport.rect.width + 2, "Content clips horizontally.");
            foreach (var row in t.ui.uiCanvas.GetComponentsInChildren<HorizontalLayoutGroup>(false))
            {
                var children = Enumerable.Range(0, row.transform.childCount).Select(i => row.transform.GetChild(i)).Where(c => c.gameObject.activeSelf).OfType<RectTransform>().ToArray();
                for (int i = 0; i < children.Length; i++)
                {
                    var rect = Relative(children[i], (RectTransform)row.transform); t.Check(rect.width > 0 && rect.height > 0, "Collapsed controls in " + row.name);
                    if (i > 0) t.Check(Relative(children[i - 1], (RectTransform)row.transform).xMax <= rect.xMin + 1, "Overlapping controls in " + row.name);
                    if (children[i].GetComponent<Button>()) t.Check(rect.height >= 32, "Button hit area too short: " + row.name);
                }
            }
            t.report.layoutChecks++;
        }
        private static Rect Relative(RectTransform child, RectTransform parent)
        { var corners = new Vector3[4]; child.GetWorldCorners(corners); var a = parent.InverseTransformPoint(corners[0]); var b = parent.InverseTransformPoint(corners[2]); return Rect.MinMaxRect(a.x, a.y, b.x, b.y); }
        private static bool Contains(Rect a, Rect b) { return b.xMin >= a.xMin - 2 && b.yMin >= a.yMin - 2 && b.xMax <= a.xMax + 2 && b.yMax <= a.yMax + 2; }
        private static T Read<T>(object o, string field) { return (T)o.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(o); }
        private static void Write(object o, string field, object value) { o.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(o, value); }
        private static string Json(Appearance a) { return JsonUtility.ToJson(a); }
    }
}

