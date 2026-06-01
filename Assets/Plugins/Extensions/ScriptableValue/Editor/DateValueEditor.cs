using UnityEditor;

namespace Extensions.ScriptableValues.Editor
{
    /// <summary>
    /// Editor для DateValue
    /// </summary>
    [CustomEditor(typeof(DateValue))]
    public class DateValueEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            DateValue dateValue = (DateValue)target;

            if (!dateValue.IsDefaultValueValid())
            {
                EditorGUILayout.HelpBox(
                    $"Некорректная дефолтная дата. Ожидаемый формат: {dateValue.DateFormat} (пример: 28.04.26)",
                    MessageType.Error
                );
            }
        }
    }
}