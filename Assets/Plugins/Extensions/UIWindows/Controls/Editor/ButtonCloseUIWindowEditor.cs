#if UNITY_EDITOR
using Extensions.Helpers;
using UnityEditor;
using UnityEngine;

namespace Extensions.UIWindows.Editor
{
    [CustomEditor(typeof(ButtonActionCloseUIWindow), true)]
    public class ButtonCloseUIWindowEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (target.GetType() != typeof(ButtonActionCloseUIWindow))
                return;

            GUILayout.Space(8f);

            if (GUILayout.Button("Convert To ButtonCloseUIWindowAnimated"))
            {
                Converter.ConvertComponent<ButtonActionCloseUIWindow, ButtonActionCloseUIWindowAnimated>((ButtonActionCloseUIWindow)target);
            }
        }
    }
}
#endif