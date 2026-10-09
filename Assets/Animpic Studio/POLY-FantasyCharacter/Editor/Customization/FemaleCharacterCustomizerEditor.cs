using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Animpic.FantasyCharacter.Editor
{
    [CustomEditor(typeof(FemaleCharacterCustomizer))]
    public sealed class FemaleCharacterCustomizerEditor : UnityEditor.Editor
    {
        private static readonly string[] Styles = { "A", "B", "C", "D" };
        private static readonly string[] Forearms = { "Match torso", "A", "B", "C", "D" };
        private static readonly string[] Shoes = { "Bare feet", "A - Tall boots", "B - Tall boots", "C - Tall boots", "D - Tall boots", "E - Low shoes", "F - Low shoes" };
        private static readonly string[] Gloves = { "None", "A", "B", "C", "D" };
        private string error;

        public override void OnInspectorGUI()
        {
            var character = (FemaleCharacterCustomizer)target;
            if (!character.Catalog)
            {
                EditorGUILayout.HelpBox("Set up this character using Tools > Animpic > Fantasy Female > Set up customization.", MessageType.Info);
                if (GUILayout.Button("Set up this character")) { Undo.RegisterFullObjectHierarchyUndo(character.gameObject, "Set up female customization"); FemaleCustomizationSetup.Configure(character.gameObject, FemaleCustomizationSetup.BuildCatalog(), true); Dirty(character); }
                return;
            }
            var s = character.CaptureSelection();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Appearance", EditorStyles.boldLabel);
            s.torso = EditorGUILayout.Popup("Torso", s.torso, Styles);
            s.pants = EditorGUILayout.Popup("Trousers", s.pants, Styles);
            s.footwear = EditorGUILayout.Popup("Footwear", s.footwear, Shoes);
            EditorGUILayout.LabelField("Trouser length", s.footwear >= 1 && s.footwear <= 4 ? "Short - automatic" : "Long - automatic");
            s.hair = EditorGUILayout.Popup("Hair", s.hair, new[] { "None", "D", "E", "F" });
            s.brows = EditorGUILayout.Popup("Eyebrows", s.brows, new[] { "None", "A", "B", "C" });
            s.palette = EditorGUILayout.Popup("Palette", s.palette, new[] { "A", "B", "C", "D", "E", "F" });
            EditorGUILayout.Space();
            Arm("Left arm", ref s.leftArmMode, ref s.leftForearmStyle, ref s.leftGlove);
            Arm("Right arm", ref s.rightArmMode, ref s.rightForearmStyle, ref s.rightGlove);
            if (EditorGUI.EndChangeCheck()) Apply(character, s);
            EditorGUILayout.Space();
            if (GUILayout.Button("Randomize"))
            {
                Record(character);
                if (!character.Randomize(unchecked((int)System.DateTime.UtcNow.Ticks), out error)) Debug.LogWarning(error, character);
                Dirty(character);
            }
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Copy appearance")) EditorGUIUtility.systemCopyBuffer = character.SaveJson();
            if (GUILayout.Button("Paste appearance"))
            {
                Record(character);
                if (character.TryLoadJson(EditorGUIUtility.systemCopyBuffer, out error)) Dirty(character);
            }
            EditorGUILayout.EndHorizontal();
            if (!string.IsNullOrEmpty(error)) EditorGUILayout.HelpBox(error, MessageType.Error);
        }

        private static void Arm(string title, ref FemaleArmMode mode, ref int forearm, ref int glove)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            mode = (FemaleArmMode)EditorGUILayout.EnumPopup("Construction", mode);
            using (new EditorGUI.DisabledScope(mode != FemaleArmMode.Split || glove != 0))
                forearm = EditorGUILayout.Popup("Forearm", forearm + 1, Forearms) - 1;
            glove = EditorGUILayout.Popup("Glove", glove, Gloves);
            if (glove != 0) EditorGUILayout.HelpBox("This glove replaces the forearm and hand. Your arm choice returns when the glove is removed.", MessageType.None);
        }

        private void Apply(FemaleCharacterCustomizer character, FemaleCustomizationSelection selection)
        {
            Record(character);
            if (character.TryApply(selection, out error)) Dirty(character);
        }
        private static void Record(FemaleCharacterCustomizer character)
        {
            var renderers = character.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Object[] objects = new Object[renderers.Length + 1];
            objects[0] = character;
            for (int i = 0; i < renderers.Length; i++) objects[i + 1] = renderers[i];
            Undo.RecordObjects(objects, "Customize fantasy female");
        }
        private static void Dirty(FemaleCharacterCustomizer character)
        {
            EditorUtility.SetDirty(character);
            if (PrefabUtility.IsPartOfPrefabInstance(character)) PrefabUtility.RecordPrefabInstancePropertyModifications(character);
            foreach (var r in character.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (PrefabUtility.IsPartOfPrefabInstance(r)) PrefabUtility.RecordPrefabInstancePropertyModifications(r);
            if (!Application.isPlaying && character.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(character.gameObject.scene);
            SceneView.RepaintAll();
        }
    }
}
