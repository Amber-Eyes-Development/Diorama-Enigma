using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DioramaEnigma.Riddles.Editor
{
    [CustomEditor(typeof(BoolPuzzleStep))]
    internal sealed class BoolPuzzleStepEditor : PuzzleStepEditorBase { }

    [CustomEditor(typeof(StateSetPuzzleStep))]
    internal sealed class StateSetPuzzleStepEditor : PuzzleStepEditorBase { }

    [CustomEditor(typeof(StringPuzzleStep))]
    internal sealed class StringPuzzleStepEditor : PuzzleStepEditorBase { }

    [CustomEditor(typeof(CompositePuzzleStep))]
    internal sealed class CompositePuzzleStepEditor : PuzzleStepEditorBase { }

    /// <summary>
    /// Базовый инспектор ассета-шага: стандартный инспектор + секция «Используется в»
    /// </summary>
    internal abstract class PuzzleStepEditorBase : UnityEditor.Editor
    {
        private const double RefreshIntervalSeconds = 5.0;

        private readonly List<(PuzzleSequence seq, int entryIndex, int groupIndex)> usedIn = new();
        private double lastRefreshTime = double.MinValue;

        private GUIStyle styleSmall;

        private void OnEnable() => RefreshUsedIn();

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (EditorApplication.timeSinceStartup - lastRefreshTime > RefreshIntervalSeconds)
                RefreshUsedIn();

            EditorGUILayout.Space(10);
            DrawUsedInSection();
        }

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

            foreach (var (seq, entryIndex, groupIndex) in usedIn)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                EditorGUILayout.LabelField($"  {seq.name}   группа {groupIndex}", styleSmall);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("↗", GUILayout.Width(24), GUILayout.Height(16)))
                    RiddleSystemEditorWindow.Open(seq, entryIndex);

                if (GUILayout.Button("Ping", GUILayout.Width(44), GUILayout.Height(16)))
                {
                    Selection.activeObject = seq;
                    EditorGUIUtility.PingObject(seq);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        #endregion

        #region Scan

        private void RefreshUsedIn()
        {
            lastRefreshTime = EditorApplication.timeSinceStartup;
            usedIn.Clear();

            var guids = AssetDatabase.FindAssets("t:PuzzleSequence");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var seq = AssetDatabase.LoadAssetAtPath<PuzzleSequence>(path);
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
