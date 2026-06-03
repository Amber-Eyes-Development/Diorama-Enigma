using UnityEditor;
using UnityEngine;

namespace DioramaEnigma.Riddles.Editor
{
    /// <summary>
    /// Инспектор PuzzleRunner с debug-панелью в Play Mode
    /// </summary>
    [CustomEditor(typeof(PuzzleRunner))]
    public sealed class PuzzleRunnerEditor : UnityEditor.Editor
    {
        private void OnEnable() => EditorApplication.update += Repaint;
        private void OnDisable() => EditorApplication.update -= Repaint;

        private static readonly Color ColorGroupLabel = new(0.6f, 0.9f, 1f);
        private static readonly Color ColorActiveStep = new(0.4f, 1f, 0.5f);
        private static readonly Color ColorButtonSkip = new(0.3f, 0.7f, 1f);
        private static readonly Color ColorButtonRestart = new(1f, 0.6f, 0.3f);

        private GUIStyle groupLabelStyle;
        private GUIStyle stepLabelStyle;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (!Application.isPlaying) return;

            EnsureStyles();

            var runner = (PuzzleRunner)target;

            EditorGUILayout.Space(10);
            DrawSeparator(new Color(0.5f, 0.5f, 0.5f));
            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField("Debug (Play Mode)", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            DrawCurrentState(runner);

            EditorGUILayout.Space(6);

            DrawControls(runner);
        }

        private void DrawCurrentState(PuzzleRunner runner)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Группа:", GUILayout.Width(60));
            EditorGUILayout.LabelField(runner.Editor_CurrentGroupIndex.ToString(), groupLabelStyle);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Активные шаги:", EditorStyles.miniLabel);

            bool hasActiveSteps = false;
            foreach (var step in runner.Editor_ActiveSteps)
            {
                hasActiveSteps = true;
                string label = string.IsNullOrEmpty(step.StepLabel) ? "(без названия)" : step.StepLabel;
                EditorGUILayout.LabelField($"  ● {label}", stepLabelStyle);
            }

            if (!hasActiveSteps)
                EditorGUILayout.LabelField("  (нет активных шагов)", EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
        }

        private void DrawControls(PuzzleRunner runner)
        {
            var prevColor = GUI.backgroundColor;

            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = ColorButtonSkip;
            if (GUILayout.Button("⏩  Пропустить группу", GUILayout.Height(28)))
            {
                runner.Editor_ForceCompleteCurrentGroup();
                EditorUtility.SetDirty(runner);
            }

            GUI.backgroundColor = ColorButtonRestart;
            if (GUILayout.Button("↺  Перезапустить", GUILayout.Height(28)))
            {
                runner.RestartSequence();
                EditorUtility.SetDirty(runner);
            }

            GUI.backgroundColor = prevColor;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);

            if (GUILayout.Button("↺  Перезапустить шаг", GUILayout.Height(22)))
            {
                runner.RestartStep();
                EditorUtility.SetDirty(runner);
            }
        }

        private void EnsureStyles()
        {
            groupLabelStyle ??= new GUIStyle(EditorStyles.boldLabel)
                { normal = { textColor = ColorGroupLabel } };

            stepLabelStyle ??= new GUIStyle(EditorStyles.miniLabel)
                { normal = { textColor = ColorActiveStep } };
        }

        private static void DrawSeparator(Color color)
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, color);
        }
    }
}
