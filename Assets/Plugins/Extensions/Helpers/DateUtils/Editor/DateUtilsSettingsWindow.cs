using Extensions.Log;
using UnityEditor;
using UnityEngine;

namespace Extensions.Helpers.Editor
{
    /// <summary>
    /// Окно настроек DateUtils
    /// </summary>
    public class DateUtilsSettingsWindow : EditorWindow
    {
        private string dateFormat;
        private int twoDigitYearMax;

        [MenuItem("Tools/Date Utils Settings")]
        private static void Open()
        {
            GetWindow<DateUtilsSettingsWindow>("Date Utils Settings");
        }

        private void OnEnable()
        {
            DateUtils.LoadSettings();

            dateFormat = DateUtils.DateFormat;
            twoDigitYearMax = DateUtils.TwoDigitYearMax;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Date Utils Settings", EditorStyles.boldLabel);

            dateFormat = EditorGUILayout.TextField("Date Format", dateFormat);
            twoDigitYearMax = EditorGUILayout.IntField("Two Digit Year Max", twoDigitYearMax);

            EditorGUILayout.Space();

            if (string.IsNullOrWhiteSpace(dateFormat))
                EditorGUILayout.HelpBox("Формат даты пустой. Будет использован дефолтный формат.", MessageType.Warning);

            if (twoDigitYearMax < 2000)
                EditorGUILayout.HelpBox("Максимальный год должен быть не меньше 2000.", MessageType.Warning);

            EditorGUILayout.Space();

            if (GUILayout.Button("Save"))
            {
                DateUtils.SaveSettings(dateFormat, twoDigitYearMax);
                ServiceDebug.LogWarning($"Настройки сохранены (формат: {dateFormat}, макс. дата: {twoDigitYearMax})");
            }

            if (GUILayout.Button("Reset To Default"))
            {
                DateUtilsSettings defaultSettings = new DateUtilsSettings();

                dateFormat = defaultSettings.DateFormat;
                twoDigitYearMax = defaultSettings.TwoDigitYearMax;

                DateUtils.SaveSettings(dateFormat, twoDigitYearMax);
                ServiceDebug.LogWarning($"Настройки сброшены до дефолтных (формат: {dateFormat}, макс. дата: {twoDigitYearMax})");

                Repaint();
            }
        }
    }
}