using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Animpic.CharacterStudio.Editor
{
    public sealed class CharacterStudioWindow : EditorWindow
    {
        private CharacterCustomizer selected;
        private Vector2 scroll;
        private int paintedIndex;
        private string error;

        [MenuItem("Tools/Animpic Studio/Characters/Fantasy Character/Open Studio", false, 300)]
        public static void Open()
        {
            var window = GetWindow<CharacterStudioWindow>();
            window.titleContent = new GUIContent("Character Studio");
            window.minSize = new Vector2(410, 540);
            window.Show();
        }
        private void OnEnable() { Undo.undoRedoPerformed += OnUndo; Selection.selectionChanged += OnSelection; OnSelection(); }
        private void OnDisable() { Undo.undoRedoPerformed -= OnUndo; Selection.selectionChanged -= OnSelection; }
        private void OnUndo() { Repaint(); }
        private void OnSelection()
        {
            var go = Selection.activeGameObject;
            if (go)
            {
                var candidate = go.GetComponentInParent<CharacterCustomizer>();
                if (candidate && candidate.gameObject.scene.IsValid()) { selected = candidate; paintedIndex = 0; ActivateStudioCharacter(selected); }
            }
            Repaint();
        }
        private static void ActivateStudioCharacter(CharacterCustomizer character)
        {
            if (!character || character.gameObject.scene.name != "CharacterStudio") return;
            var ui = Object.FindObjectsOfType<CharacterStudioUI>(true).FirstOrDefault(u => u.gameObject.scene == character.gameObject.scene && (u.female == character || u.male == character));
            if (!ui) return;
            bool changed = ui.female.gameObject.activeSelf != (character == ui.female) || ui.male.gameObject.activeSelf != (character == ui.male);
            if (changed) Undo.RecordObjects(new Object[] { ui.female.gameObject, ui.male.gameObject }, "Select studio character");
            ui.SetCharacter(character == ui.male);
            if (changed && !Application.isPlaying)
            {
                foreach (var c in new[] { ui.female, ui.male })
                    if (PrefabUtility.IsPartOfPrefabInstance(c.gameObject)) PrefabUtility.RecordPrefabInstancePropertyModifications(c.gameObject);
                EditorSceneManager.MarkSceneDirty(character.gameObject.scene);
            }
        }
        private void OnGUI()
        {
            using (new CharacterStudioEditorUI.SkinScope())
            {
                EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), AnimpicStudioTheme.Background);
                CharacterStudioEditorUI.Header("Character Studio", "Build a character. Refine every detail.");
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (CharacterStudioEditorUI.Button("Open studio scene", true)) CharacterStudioSetup.OpenStudio();
                    if (CharacterStudioEditorUI.Button("Select character") && selected) Selection.activeGameObject = selected.gameObject;
                }
                EditorGUILayout.Space(12);
                var characters = Object.FindObjectsOfType<CharacterCustomizer>(true).Where(c => c.gameObject.scene.IsValid() && c.gameObject.scene.isLoaded && !EditorUtility.IsPersistent(c)).ToArray();
                if (characters.Length == 0)
                {
                    EditorGUILayout.HelpBox("Open the studio scene or add a customizable character to a scene.", MessageType.Info);
                    return;
                }
                if (!selected || !characters.Contains(selected)) selected = characters.FirstOrDefault(c => c.gameObject.activeSelf) ?? characters[0];
                int active = Array.IndexOf(characters, selected);
                string[] labels = characters.Select(c => (c.Catalog ? c.Catalog.label : c.name) + "  ·  " + c.gameObject.scene.name).ToArray();
                int next = EditorGUILayout.Popup("Character", active, labels);
                if (next != active) { selected = characters[next]; paintedIndex = 0; error = null; ActivateStudioCharacter(selected); }
                if (!selected.Catalog) { EditorGUILayout.HelpBox("This character has no catalogue assigned.", MessageType.Warning); return; }
                using (var area = new EditorGUILayout.ScrollViewScope(scroll))
                {
                    scroll = area.scrollPosition;
                    CharacterStudioEditorUI.Draw(selected, ref paintedIndex, ref error);
                }
            }
        }
    }

    [InitializeOnLoad]
    internal static class CharacterStudioEditorUI
    {
        static CharacterStudioEditorUI() { Undo.undoRedoPerformed += RefreshAfterUndo; }
        private static readonly string[] PaletteNames = { "Palette A", "Palette B", "Palette C", "Palette D", "Palette E", "Palette F" };
        private static readonly string[] OverridePaletteNames = { "Use character palette", "Palette A", "Palette B", "Palette C", "Palette D", "Palette E", "Palette F" };
        private static GUIStyle wordmarkStyle, titleStyle, mutedStyle, sectionStyle, smallHeadingStyle, buttonStyle;
        private static readonly GUIStyle CardLayout = new GUIStyle { padding = new RectOffset(13, 13, 8, 11), margin = new RectOffset(4, 4, 1, 1) };
        private static void EnsureStyles()
        {
            if (wordmarkStyle != null) return;
            wordmarkStyle = TextStyle(17, AnimpicStudioTheme.Text, AnimpicStudioTheme.HeadingFont, FontStyle.Bold);
            titleStyle = TextStyle(21, AnimpicStudioTheme.Text, AnimpicStudioTheme.HeadingFont, FontStyle.Bold);
            mutedStyle = TextStyle(11, AnimpicStudioTheme.Muted, AnimpicStudioTheme.BodyFont); mutedStyle.wordWrap = true;
            sectionStyle = TextStyle(14, AnimpicStudioTheme.Text, AnimpicStudioTheme.HeadingFont, FontStyle.Bold);
            smallHeadingStyle = TextStyle(12, AnimpicStudioTheme.Muted, AnimpicStudioTheme.MediumFont);
            buttonStyle = new GUIStyle(GUI.skin.button) { font = AnimpicStudioTheme.MediumFont, fontSize = 12, alignment = TextAnchor.MiddleCenter, padding = new RectOffset(8, 8, 0, 0) };
            foreach (var state in new[] { buttonStyle.normal, buttonStyle.hover, buttonStyle.active, buttonStyle.focused, buttonStyle.onNormal, buttonStyle.onHover, buttonStyle.onActive, buttonStyle.onFocused }) state.background = null;
        }
        private static GUIStyle TextStyle(int size, Color color, Font font, FontStyle weight = FontStyle.Normal)
        {
            var style = new GUIStyle(EditorStyles.label) { font = font, fontSize = size, fontStyle = weight, clipping = TextClipping.Clip };
            style.normal.textColor = color; return style;
        }
        internal sealed class SkinScope : IDisposable
        {
            private readonly Color labelColor, contentColor;
            private readonly Font labelFont;
            internal SkinScope()
            {
                EnsureStyles();
                labelColor = EditorStyles.label.normal.textColor; labelFont = EditorStyles.label.font; contentColor = GUI.contentColor;
                // Prefix labels must remain readable on the dark cards even with
                // Unity's light skin; native popup/color controls keep their skin.
                EditorStyles.label.normal.textColor = AnimpicStudioTheme.Text;
                EditorStyles.label.font = AnimpicStudioTheme.BodyFont; GUI.contentColor = Color.white;
                var bounds = EditorGUILayout.BeginVertical(GUIStyle.none);
                if (Event.current.type == EventType.Repaint) EditorGUI.DrawRect(bounds, AnimpicStudioTheme.Background);
            }
            public void Dispose()
            {
                EditorGUILayout.EndVertical();
                EditorStyles.label.normal.textColor = labelColor; EditorStyles.label.font = labelFont; GUI.contentColor = contentColor;
            }
        }
        internal sealed class CardScope : IDisposable
        {
            internal CardScope()
            {
                EnsureStyles(); var rect = EditorGUILayout.BeginVertical(CardLayout);
                if (Event.current.type == EventType.Repaint)
                {
                    EditorGUI.DrawRect(rect, AnimpicStudioTheme.Border);
                    EditorGUI.DrawRect(new Rect(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 2), AnimpicStudioTheme.Panel);
                }
            }
            public void Dispose() { EditorGUILayout.EndVertical(); }
        }
        internal static void Header(string title, string subtitle)
        {
            EnsureStyles();
            Rect rect = GUILayoutUtility.GetRect(0, 140, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, AnimpicStudioTheme.Background);
            var wordmark = AnimpicStudioTheme.Wordmark;
            if (wordmark && wordmark.texture)
            {
                // Official 833 x 276 artwork: visible alpha bounds start at
                // (50, 46) and span 725 x 159. Account for transparent padding
                // when aligning the visible wordmark with the heading below.
                var texture = wordmark.texture;
                float visibleWidth = Mathf.Min(235, Mathf.Max(1, rect.width - 32));
                float width = visibleWidth * 833f / 725f;
                float height = width * texture.height / texture.width;
                var imageRect = new Rect(rect.x + 16 - width * 50f / 833f,
                    rect.y + 13 - height * 46f / 276f, width, height);
                GUI.DrawTexture(imageRect, texture, ScaleMode.ScaleToFit, true);
            }
            else
            {
                EditorGUI.DrawRect(new Rect(rect.x + 16, rect.y + 22, 3, 17), AnimpicStudioTheme.Accent);
                GUI.Label(new Rect(rect.x + 27, rect.y + 19, rect.width - 43, 24), "ANIMPIC", wordmarkStyle);
            }
            GUI.Label(new Rect(rect.x + 16, rect.y + 78, rect.width - 32, 29), title, titleStyle);
            GUI.Label(new Rect(rect.x + 17, rect.y + 111, rect.width - 34, 21), subtitle, mutedStyle);
            EditorGUI.DrawRect(new Rect(rect.x + 16, rect.yMax - 1, rect.width - 32, 1), AnimpicStudioTheme.Border);
            EditorGUILayout.Space(10);
        }
        internal static bool Button(string label, bool primary = false, int height = 32)
        {
            EnsureStyles(); var content = new GUIContent(label);
            var rect = GUILayoutUtility.GetRect(content, buttonStyle, GUILayout.Height(height), GUILayout.ExpandWidth(true));
            bool hover = GUI.enabled && rect.Contains(Event.current.mousePosition);
            Color fill = primary ? (hover ? AnimpicStudioTheme.AccentHover : AnimpicStudioTheme.Accent) : (hover ? AnimpicStudioTheme.Border : AnimpicStudioTheme.Surface);
            if (!GUI.enabled) fill = Color.Lerp(AnimpicStudioTheme.Panel, fill, 0.4f);
            string controlName = "AnimpicStudio/" + label;
            Color border = GUI.GetNameOfFocusedControl() == controlName ? AnimpicStudioTheme.AccentBlue : primary ? fill : AnimpicStudioTheme.Border;
            EditorGUI.DrawRect(rect, border);
            EditorGUI.DrawRect(new Rect(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 2), fill);
            Color text = Color.white;
            foreach (var state in new[] { buttonStyle.normal, buttonStyle.hover, buttonStyle.active, buttonStyle.focused, buttonStyle.onNormal, buttonStyle.onHover, buttonStyle.onActive, buttonStyle.onFocused }) state.textColor = text;
            GUI.SetNextControlName(controlName);
            return GUI.Button(rect, content, buttonStyle);
        }
        private static void Hint(string text) { EnsureStyles(); GUILayout.Label(text, mutedStyle); }
        internal static void Draw(CharacterCustomizer character, ref int paintedIndex, ref string error)
        {
            var catalog = character.Catalog;
            if (!catalog) { EditorGUILayout.HelpBox("Assign a catalogue before editing this character.", MessageType.Warning); return; }
            var appearance = character.Capture();
            EditorGUI.BeginChangeCheck();
            using (new CardScope())
            {
                Section("Appearance");
                appearance.hair = EditorGUILayout.Popup("Hairstyle", appearance.hair, Optional(catalog.hair.Select(o => o.label)));
                appearance.brows = EditorGUILayout.Popup("Eyebrows", appearance.brows, Optional(catalog.brows.Select(o => o.label)));
                if (catalog.beards.Length > 0) appearance.beard = EditorGUILayout.Popup("Facial hair", appearance.beard, Optional(catalog.beards.Select(o => o.label)));
                foreach (var slot in catalog.extras)
                {
                    var choice = (appearance.extras ?? new SlotChoice[0]).FirstOrDefault(x => x.id == slot.id);
                    int previous = choice == null ? 0 : choice.option;
                    int next = EditorGUILayout.Popup(slot.label, previous, Optional(slot.options.Select(o => o.label)));
                    if (next != previous)
                    {
                        var choices = new List<SlotChoice>(appearance.extras ?? new SlotChoice[0]);
                        if (choice == null) { choice = new SlotChoice { id = slot.id }; choices.Add(choice); }
                        choice.option = next; appearance.extras = choices.ToArray();
                    }
                }
                EditorGUILayout.Space(5);
            }
            EditorGUILayout.Space(6);
            using (new CardScope())
            {
                Section("Outfit");
                appearance.torso = EditorGUILayout.Popup("Torso", appearance.torso, catalog.torsos.Select(o => o.label).ToArray());
                appearance.pants = EditorGUILayout.Popup("Trousers", appearance.pants, catalog.pants.Select(o => o.label).ToArray());
                appearance.footwear = EditorGUILayout.Popup("Footwear", appearance.footwear, Optional(catalog.footwear.Select(o => o.label)));
                Hint("Trouser length follows the selected footwear.");
                EditorGUILayout.Space(7);
                Arm(character, appearance, true); EditorGUILayout.Space(8); Arm(character, appearance, false);
                EditorGUILayout.Space(5);
            }
            EditorGUILayout.Space(6);
            using (new CardScope())
            {
                Section("Colour & finish");
                appearance.basePalette = EditorGUILayout.Popup("Character palette", appearance.basePalette, PaletteNames);
                EditorGUILayout.Space(8);
                var visible = character.GetVisibleParts();
                if (visible.Length > 0)
                {
                    // Selecting a target element is a UI operation, not a character change.
                    bool changedBeforeTarget = GUI.changed;
                    paintedIndex = EditorGUILayout.Popup("Element", Mathf.Clamp(paintedIndex, 0, visible.Length - 1), visible.Select((p, i) => string.IsNullOrWhiteSpace(p.label) ? "Element " + (i + 1) : p.label).ToArray());
                    GUI.changed = changedBeforeTarget;
                    string id = visible[paintedIndex].id;
                    var paint = (appearance.paints ?? new PartPaint[0]).FirstOrDefault(p => p.partId == id);
                    int oldPalette = paint == null ? -1 : paint.palette;
                    Color oldTint = paint == null ? Color.white : paint.tint;
                    int palette = EditorGUILayout.Popup("Element palette", oldPalette + 1, OverridePaletteNames) - 1;
                    Color tint = EditorGUILayout.ColorField(new GUIContent("Element tint"), oldTint, true, false, false);
                    if (palette != oldPalette || tint != oldTint)
                    {
                        var paints = new List<PartPaint>(appearance.paints ?? new PartPaint[0]);
                        if (paint == null) { paint = new PartPaint { partId = id }; paints.Add(paint); }
                        paint.palette = palette; tint.a = 1; paint.tint = tint; appearance.paints = paints.ToArray();
                    }
                    using (new EditorGUI.DisabledScope(paint == null))
                    {
                        if (Button("Reset element finish", false, 27))
                        { appearance.paints = (appearance.paints ?? new PartPaint[0]).Where(p => p.partId != id).ToArray(); GUI.changed = true; }
                    }
                    Hint(visible.Length + " visible elements · hidden element colours are retained");
                }
                EditorGUILayout.Space(5);
            }
            if (EditorGUI.EndChangeCheck() && JsonUtility.ToJson(character.Capture()) != JsonUtility.ToJson(appearance))
                Apply(character, appearance, "Customize character", out error);

            EditorGUILayout.Space(12);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (Button("Randomize", true))
                {
                    Record(character, "Randomize character");
                    string before = character.SaveJson();
                    if (character.Randomize(unchecked(Environment.TickCount ^ Guid.NewGuid().GetHashCode()), out error)) MarkChanged(character, before);
                }
                if (Button("Reset look"))
                {
                    Apply(character, new Appearance { catalogId = catalog.id, hair = catalog.hair.Length > 0 ? 1 : 0, brows = catalog.brows.Length > 0 ? 1 : 0 }, "Reset character appearance", out error);
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (Button("Save look to file", false, 29))
                {
                    string path = EditorUtility.SaveFilePanel("Save character appearance", "", catalog.id + "-look.json", "json");
                    if (!string.IsNullOrEmpty(path)) try { File.WriteAllText(path, character.SaveJson()); error = null; } catch (Exception ex) { error = ex.Message; }
                }
                if (Button("Load look from file", false, 29))
                {
                    string path = EditorUtility.OpenFilePanel("Load character appearance", "", "json");
                    if (!string.IsNullOrEmpty(path))
                    {
                        try
                        {
                            string json = File.ReadAllText(path), before = character.SaveJson();
                            Record(character, "Load character appearance");
                            if (character.TryLoadJson(json, out error)) MarkChanged(character, before);
                        }
                        catch (Exception ex) { error = ex.Message; }
                    }
                }
            }
            if (!string.IsNullOrEmpty(error)) EditorGUILayout.HelpBox(error, MessageType.Error);
            EditorGUILayout.Space(10);
        }
        private static void Arm(CharacterCustomizer character, Appearance appearance, bool left)
        {
            EnsureStyles(); GUILayout.Label(left ? "Left arm" : "Right arm", smallHeadingStyle);
            var catalog = character.Catalog;
            int mode = EditorGUILayout.Popup("Construction", (int)(left ? appearance.leftArmMode : appearance.rightArmMode), new[] { "One piece", "Two pieces" });
            string[] styles = new[] { "Match torso" }.Concat(catalog.torsos.Select(o => o.label)).ToArray();
            int forearm = EditorGUILayout.Popup("Forearm style", (left ? appearance.leftForearmStyle : appearance.rightForearmStyle) + 1, styles) - 1;
            int glove = EditorGUILayout.Popup("Glove", left ? appearance.leftGlove : appearance.rightGlove, Optional(catalog.gloves.Select(o => o.label)));
            if (left) { appearance.leftArmMode = (ArmMode)mode; appearance.leftForearmStyle = forearm; appearance.leftGlove = glove; }
            else { appearance.rightArmMode = (ArmMode)mode; appearance.rightForearmStyle = forearm; appearance.rightGlove = glove; }
            if (glove > 0) Hint("Remove the glove to restore your selected arm.");
        }
        private static void Section(string label)
        {
            EnsureStyles(); Rect rect = EditorGUILayout.GetControlRect(false, 30);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y + 9, 3, 11), AnimpicStudioTheme.Accent);
            GUI.Label(new Rect(rect.x + 11, rect.y + 3, rect.width - 11, 25), label, sectionStyle);
            EditorGUILayout.Space(3);
        }
        private static string[] Optional(IEnumerable<string> values) { return new[] { "None" }.Concat(values).ToArray(); }
        private static void Record(CharacterCustomizer character, string label)
        {
            var objects = new List<Object> { character };
            objects.AddRange(character.GetComponentsInChildren<SkinnedMeshRenderer>(true));
            Undo.RecordObjects(objects.ToArray(), label);
        }
        internal static bool Apply(CharacterCustomizer character, Appearance appearance, string label, out string error)
        {
            string before = character.SaveJson();
            if (JsonUtility.ToJson(character.Capture()) == JsonUtility.ToJson(appearance)) { error = null; return true; }
            Record(character, label);
            bool ok = character.TryApply(appearance, out error);
            if (ok) MarkChanged(character, before);
            return ok;
        }
        private static void MarkChanged(CharacterCustomizer character, string before)
        {
            if (before == character.SaveJson()) return;
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(character);
                if (PrefabUtility.IsPartOfPrefabInstance(character)) PrefabUtility.RecordPrefabInstancePropertyModifications(character);
                foreach (var renderer in character.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    EditorUtility.SetDirty(renderer);
                    if (PrefabUtility.IsPartOfPrefabInstance(renderer)) PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                }
                var scene = character.gameObject.scene;
                if (scene.IsValid() && scene.isLoaded) EditorSceneManager.MarkSceneDirty(scene);
            }
            SceneView.RepaintAll();
        }
        internal static void RefreshAfterUndo()
        {
            foreach (var character in Object.FindObjectsOfType<CharacterCustomizer>(true))
            {
                if (!character.gameObject.scene.IsValid() || EditorUtility.IsPersistent(character) || !character.Catalog) continue;
                string error;
                if (!character.TryApply(character.Capture(), out error)) Debug.LogWarning("Character appearance could not refresh after Undo: " + error, character);
            }
            SceneView.RepaintAll();
        }
    }
}
