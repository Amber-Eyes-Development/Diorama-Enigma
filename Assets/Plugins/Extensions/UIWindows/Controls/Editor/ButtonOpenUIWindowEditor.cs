#if UNITY_EDITOR
using Extensions.Helpers;
using UnityEditor;
using UnityEngine;

namespace Extensions.UIWindows.Editor
{
    [CustomEditor(typeof(ButtonActionOpenUIWindow), true)]
    public class ButtonOpenUIWindowEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            UIWindowsEditorDrawer.DrawOpenWindowInspector(serializedObject);

            serializedObject.ApplyModifiedProperties();

            if (target.GetType() != typeof(ButtonActionOpenUIWindow))
                return;

            GUILayout.Space(8f);

            if (GUILayout.Button("Convert To ButtonOpenUIWindowAnimated"))
            {
                Converter.ConvertComponent<ButtonActionOpenUIWindow, ButtonActionOpenUIWindowAnimated>((ButtonActionOpenUIWindow)target);
            }
        }
    }

    internal static class UIWindowsEditorDrawer
    {
        private const string NeedToCloseThisPropertyName = "needToCloseThis";
        private const string NeedToCloseOpenedPropertyName = "needToCloseOpened";

        public static void DrawOpenWindowInspector(SerializedObject serializedObject)
        {
            SerializedProperty needToCloseThis = serializedObject.FindProperty(NeedToCloseThisPropertyName);
            SerializedProperty needToCloseOpened = serializedObject.FindProperty(NeedToCloseOpenedPropertyName);

            SerializedProperty property = serializedObject.GetIterator();
            bool enterChildren = true;

            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (property.propertyPath == "m_Script")
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.PropertyField(property, true);
                    }

                    continue;
                }

                if (property.propertyPath == NeedToCloseThisPropertyName && needToCloseOpened != null)
                    continue;

                EditorGUILayout.PropertyField(property, true);

                if (property.propertyPath == NeedToCloseOpenedPropertyName && needToCloseThis != null)
                {
                    using (new EditorGUI.DisabledScope(needToCloseOpened.boolValue))
                    {
                        EditorGUILayout.PropertyField(needToCloseThis, true);
                    }
                }
            }
        }
    }
}
#endif