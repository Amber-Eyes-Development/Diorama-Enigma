using UnityEditor;
using UnityEngine;

namespace DioramaEnigma.Sequences.Editor
{
    /// <summary>
    /// Инспектор SequenceRunner с debug-панелью в Play Mode
    /// </summary>
    [CustomEditor(typeof(SequenceRunner))]
    public sealed class SequenceRunnerEditor : UnityEditor.Editor
    {
        private void OnEnable() => EditorApplication.update += Repaint;
        private void OnDisable() => EditorApplication.update -= Repaint;

        private static readonly Color ColorGroupLabel  = new(0.6f, 0.9f, 1f);
        private static readonly Color ColorDone        = new(0.4f, 1f, 0.5f);
        private static readonly Color ColorPending     = new(1f, 0.85f, 0.35f);
        private static readonly Color ColorButtonSkip  = new(0.3f, 0.7f, 1f);
        private static readonly Color ColorButtonReset = new(1f, 0.6f, 0.3f);

        private GUIStyle groupLabelStyle;
        private GUIStyle stepNameStyle;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (!Application.isPlaying) return;

            EnsureStyles();

            var runner = (SequenceRunner)target;

            EditorGUILayout.Space(10);
            DrawSeparator(new Color(0.5f, 0.5f, 0.5f));
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Debug (Play Mode)", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            DrawCurrentState(runner);

            EditorGUILayout.Space(6);
            DrawControls(runner);
        }

        #region State

        private void DrawCurrentState(SequenceRunner runner)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Группа:", GUILayout.Width(60));
            EditorGUILayout.LabelField(runner.Editor_CurrentGroupIndex.ToString(), groupLabelStyle);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Активные шаги:", EditorStyles.miniLabel);

            bool any = false;
            foreach (var step in runner.Editor_ActiveSteps)
            {
                any = true;
                DrawActiveStep(step);
            }

            if (!any)
                EditorGUILayout.LabelField("  (нет активных шагов)", EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
        }

        private void DrawActiveStep(AbstractSequenceStep step)
        {
            bool done = step.IsCompleted;
            string name = string.IsNullOrEmpty(step.StepLabel) ? "(без названия)" : step.StepLabel;
            string progress = GetStepProgressInfo(step);

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            var prevColor = GUI.contentColor;
            GUI.contentColor = done ? ColorDone : Color.white;
            EditorGUILayout.LabelField($"  ● {name}", stepNameStyle);
            GUI.contentColor = prevColor;

            if (!string.IsNullOrEmpty(progress))
            {
                prevColor = GUI.contentColor;
                GUI.contentColor = done ? ColorDone : ColorPending;
                EditorGUILayout.LabelField(progress, EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
                GUI.contentColor = prevColor;
            }

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Progress info

        private static string GetStepProgressInfo(AbstractSequenceStep step)
        {
            if (step == null) return string.Empty;

            if (step is SequenceStep valueStep)
            {
                var so = new SerializedObject(valueStep);
                var prop = so.FindProperty("completionState");
                string target = prop != null ? prop.boolValue.ToString().ToLower() : "?";
                return $"val:{valueStep.Value}  →  {target}";
            }

            if (step is CompositeSequenceStep)
            {
                var so = new SerializedObject(step);
                var childrenProp = so.FindProperty("children");
                if (childrenProp == null) return string.Empty;

                int total = childrenProp.arraySize;
                int done = 0;
                for (int i = 0; i < total; i++)
                    if (childrenProp.GetArrayElementAtIndex(i).objectReferenceValue is AbstractSequenceStep child && child.IsCompleted)
                        done++;
                return $"{done}/{total} дочерних";
            }

            return string.Empty;
        }

        #endregion

        #region Controls

        private void DrawControls(SequenceRunner runner)
        {
            var prevColor = GUI.backgroundColor;

            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = ColorButtonSkip;
            if (GUILayout.Button("⏩  Пропустить группу", GUILayout.Height(28)))
            {
                runner.Editor_ForceCompleteCurrentGroup();
                EditorUtility.SetDirty(runner);
            }

            GUI.backgroundColor = ColorButtonReset;
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

        #endregion

        #region Helpers

        private void EnsureStyles()
        {
            groupLabelStyle ??= new GUIStyle(EditorStyles.boldLabel)
                { normal = { textColor = ColorGroupLabel } };

            stepNameStyle ??= new GUIStyle(EditorStyles.miniLabel)
                { normal = { textColor = ColorDone } };
        }

        private static void DrawSeparator(Color color)
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, color);
        }

        #endregion
    }
}
