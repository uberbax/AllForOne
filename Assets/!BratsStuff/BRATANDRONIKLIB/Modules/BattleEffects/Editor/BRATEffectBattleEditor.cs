using UnityEditor;
using UnityEngine;

namespace BRATANDRONIKLIB.Editor
{
    [CustomEditor(typeof(BRATEffectBattle))]
    public sealed class BRATEffectBattleEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "Вызовы из Button.onClick или кода:\n" +
                "PlayStrike() — обычный удар Hero → Enemy.\n" +
                "PlayHeavyStrike() — усиленный удар Hero → Enemy.\n" +
                "PlayHeal() — лечение Hero персонажем Healer.\n" +
                "BRATViewBattleReset.ResetScene() — остановка эффекта и возврат исходного состояния.\n\n" +
                "Назначьте камеру, SpriteRenderer участников и оба материала до первого включения объекта. " +
                "Scene Sprites необязателен: пустой массив и элементы None пропускаются. Добавленные спрайты фона затемняются/размываются; отдельно не двигаются. " +
                "В соседнем BRATViewBattleReset укажите ту же камеру и корни героев/фона. " +
                "Корни и нужные спрайты должны быть активны при Awake. Вызовы эффектов работают только в Play Mode на активном компоненте. " +
                "Эффекты визуальные: урон и лечение в игровой модели не применяются.",
                MessageType.Info);
            DrawDefaultInspector();
            var effect = (BRATEffectBattle)target;
            using (new EditorGUI.DisabledScope(!Application.isPlaying || !effect.isActiveAndEnabled))
            {
                if (GUILayout.Button(new GUIContent("PlayStrike()", "Обычный удар: приближение камеры, выпад героя и вспышка попадания.")))
                    effect.PlayStrike();
                if (GUILayout.Button(new GUIContent("PlayHeavyStrike()", "Усиленный удар с более сильной тряской и визуальным акцентом.")))
                    effect.PlayHeavyStrike();
                if (GUILayout.Button(new GUIContent("PlayHeal()", "Визуальный эффект лечения: Healer → Hero.")))
                    effect.PlayHeal();
                if (GUILayout.Button(new GUIContent("ResetScene()", "Остановить эффект и вернуть камеру, участников и фон к состоянию Awake.")))
                    effect.GetComponent<BRATViewBattleReset>().ResetScene();
            }
        }
    }
}
