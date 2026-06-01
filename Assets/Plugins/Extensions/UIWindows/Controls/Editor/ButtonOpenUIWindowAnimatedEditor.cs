#if UNITY_EDITOR
using Extensions.Helpers;
using UnityEditor;
using UnityEngine;

namespace Extensions.UIWindows.Editor
{
    [CustomEditor(typeof(ButtonActionOpenUIWindowAnimated))]
    public class ButtonOpenUIWindowAnimatedEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            UIWindowsEditorDrawer.DrawOpenWindowInspector(serializedObject);

            serializedObject.ApplyModifiedProperties();

            ButtonActionOpenUIWindowAnimated buttonAction = (ButtonActionOpenUIWindowAnimated)target;

            if (buttonAction.OpenMode != UIWindowOpenMode.Pop)
            {
                EditorGUILayout.HelpBox(
                    "Анимация открытия используется только в режиме Pop. В режиме Forward она не применяется.",
                    MessageType.Warning
                );
            }

            GUILayout.Space(8f);

            if (GUILayout.Button("Convert To ButtonOpenUIWindow"))
            {
                Converter.ConvertComponent<ButtonActionOpenUIWindowAnimated, ButtonActionOpenUIWindow>((ButtonActionOpenUIWindowAnimated)target);
            }
        }
    }
}
#endif