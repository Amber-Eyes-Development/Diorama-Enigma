using TMPro;
using UnityEngine;

namespace DioramaEnigma.Statistics
{
    /// <summary>
    /// Строка статистики: название диорамы и затраченное на неё время
    /// </summary>
    public sealed class DioramaTimeRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private TMP_Text time;

        /// <summary> Заполнить строку </summary>
        /// <param name="title">Название диорамы</param>
        /// <param name="seconds">Время, секунд</param>
        public void Bind(string title, float seconds)
        {
            if (label != null) label.text = title;
            if (time != null) time.text = DioramaTimeFormat.Clock(seconds);
        }
    }
}
