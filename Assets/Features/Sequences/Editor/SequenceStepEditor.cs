using System.Collections.Generic;
using Extensions.EditorTools;
using UnityEditor;
using UnityEngine;

namespace DioramaEnigma.Sequences.Editor
{
    [CustomEditor(typeof(SequenceStep))]
    internal sealed class SequenceStepInspector : SequenceStepEditorBase { }

    [CustomEditor(typeof(CompositeSequenceStep))]
    internal sealed class CompositeSequenceStepEditor : SequenceStepEditorBase { }

    /// <summary>
    /// Базовый инспектор ассета-шага: стандартный инспектор + секция «Используется в»
    /// </summary>
    internal abstract class SequenceStepEditorBase : UnityEditor.Editor
    {
        private const double RefreshIntervalSeconds = 5.0;
        private const float SQUARE = EditorToolsConstraints.BASE_ELEMENT_HEIGHT;

        private static readonly Color ColorOpen = new(1f, 0.52f, 0.05f);

        private readonly List<(Sequence seq, int entryIndex, int groupIndex)> usedIn = new();
        private double lastRefreshTime = double.MinValue;

        private GUIStyle styleSmall;

        private void OnEnable() => RefreshUsedIn();

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            DrawStatePreview();

            if (EditorApplication.timeSinceStartup - lastRefreshTime > RefreshIntervalSeconds)
                RefreshUsedIn();

            EditorGUILayout.Space(10);
            DrawUsedInSection();
        }

        #region State preview

        /// <summary> Текущее состояние шага: read-only превью (цель/значение/завершён) </summary>
        private void DrawStatePreview()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Состояние (превью)", EditorStyles.miniBoldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                if (target is SequenceStep valueStep)
                {
                    EditorGUILayout.Toggle("Цель завершения", valueStep.CompletionState);
                    EditorGUILayout.Toggle("Текущее значение", valueStep.Value);
                }

                if (target is AbstractSequenceStep step)
                    EditorGUILayout.Toggle("Завершён", step.IsCompleted);
            }

            if (Application.isPlaying && target is AbstractSequenceStep playStep)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.Toggle("Разблокирован", playStep.IsUnlocked);
            }
        }

        #endregion

        #region Used-in section

        private void DrawUsedInSection()
        {
            styleSmall ??= new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };

            var rect = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 1f));
            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Используется в:", EditorStyles.boldLabel);
            if (GUILayout.Button("↺", GUILayout.Width(24), GUILayout.Height(18)))
                RefreshUsedIn();
            EditorGUILayout.EndHorizontal();

            if (usedIn.Count == 0)
            {
                EditorGUILayout.LabelField("  (ни в одной последовательности)", styleSmall);
                return;
            }

            foreach (var (seq, _, groupIndex) in usedIn)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                EditorGUILayout.LabelField($"  {seq.name}   группа {groupIndex}", styleSmall);

                GUILayout.FlexibleSpace();

                DrawOpenButton(seq);
                DrawPingButton(seq);

                EditorGUILayout.EndHorizontal();
            }
        }

        /// <summary> Открыть последовательность в инспекторе </summary>
        private static void DrawOpenButton(Object asset)
        {
            GUI.backgroundColor = ColorOpen;
            var content = EditorGUIUtility.IconContent(EditorToolsConstraints.ICON_INSPECT);
            content.tooltip = "Открыть в инспекторе";
            if (GUILayout.Button(content, GUILayout.Width(SQUARE), GUILayout.Height(SQUARE)))
            {
                Selection.activeObject = asset;
                EditorApplication.ExecuteMenuItem("Window/General/Inspector");
            }
            GUI.backgroundColor = Color.white;
        }

        /// <summary> Показать ассет в дереве проекта (ping) </summary>
        private static void DrawPingButton(Object asset)
        {
            GUI.backgroundColor = EditorToolsConstraints.COLOR_CYAN;
            if (GUILayout.Button(EditorToolsConstraints.SYMBOL_PING, GUILayout.Width(SQUARE), GUILayout.Height(SQUARE)))
                EditorGUIUtility.PingObject(asset);
            GUI.backgroundColor = Color.white;
        }

        #endregion

        #region Scan

        private void RefreshUsedIn()
        {
            lastRefreshTime = EditorApplication.timeSinceStartup;
            usedIn.Clear();

            var guids = AssetDatabase.FindAssets($"t:{nameof(Sequence)}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var seq = AssetDatabase.LoadAssetAtPath<Sequence>(path);
                if (seq == null) continue;

                var seqSO = new SerializedObject(seq);
                var stepsProp = seqSO.FindProperty("steps");

                for (int i = 0; i < stepsProp.arraySize; i++)
                {
                    var stepEntry = stepsProp.GetArrayElementAtIndex(i);
                    if (stepEntry.FindPropertyRelative("step").objectReferenceValue != target) continue;

                    int groupIndex = stepEntry.FindPropertyRelative("groupIndex").intValue;
                    usedIn.Add((seq, i, groupIndex));
                }
            }
        }

        #endregion
    }
}
