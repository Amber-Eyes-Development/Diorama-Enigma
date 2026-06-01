#if UNITY_EDITOR
using Extensions.Helpers;
using UnityEditor;
using UnityEngine;

namespace Extensions.UIWindows.Editor
{
    [CustomEditor(typeof(ButtonActionCloseUIWindowAnimated))]
    public class ButtonCloseUIWindowAnimatedEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(8f);

            if (GUILayout.Button("Convert To ButtonCloseUIWindow"))
            {
                Converter.ConvertComponent<ButtonActionCloseUIWindowAnimated, ButtonActionCloseUIWindow>((ButtonActionCloseUIWindowAnimated)target);
            }
        }
    }
}
#endif