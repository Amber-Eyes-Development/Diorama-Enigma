using StarletBooking.UI;
using UnityEditor;

[CustomEditor(typeof(DropdownTemplateResizer))]
public sealed class DropdownTemplateResizerEditor : Editor
{
    private SerializedProperty content;

    private SerializedProperty stretchToScreenBottom;
    private SerializedProperty bottomPadding;

    private SerializedProperty useFixedWidth;
    private SerializedProperty fixedWidth;
    private SerializedProperty stretchToScreenRight;
    private SerializedProperty rightPadding;

    private void OnEnable()
    {
        content = serializedObject.FindProperty("content");

        stretchToScreenBottom = serializedObject.FindProperty("stretchToScreenBottom");
        bottomPadding = serializedObject.FindProperty("bottomPadding");

        useFixedWidth = serializedObject.FindProperty("useFixedWidth");
        fixedWidth = serializedObject.FindProperty("fixedWidth");
        stretchToScreenRight = serializedObject.FindProperty("stretchToScreenRight");
        rightPadding = serializedObject.FindProperty("rightPadding");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(content);
        EditorGUILayout.PropertyField(stretchToScreenBottom);

        if (stretchToScreenBottom.boolValue) EditorGUILayout.PropertyField(bottomPadding);

        EditorGUILayout.PropertyField(useFixedWidth);

        if (useFixedWidth.boolValue)
            EditorGUILayout.PropertyField(fixedWidth);
        else
        {
            EditorGUILayout.PropertyField(stretchToScreenRight);
            if (stretchToScreenRight.boolValue) EditorGUILayout.PropertyField(rightPadding);
        }

        serializedObject.ApplyModifiedProperties();
    }
}