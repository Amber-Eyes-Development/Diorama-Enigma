using System;
using System.Diagnostics;
using UnityEngine;

namespace Extensions.Attributes
{
    /// <summary>
    /// Слайдер в диапазоне [min, max] с ограничением только снизу (разрешен ввод выше max)
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    [Conditional("UNITY_EDITOR")]
    public sealed class SoftRangeAttribute : PropertyAttribute
    {
        public readonly float Min;
        public readonly float Max;

        /// <summary>Знаков после запятой для float-поля; -1 — без округления.</summary>
        public readonly int Precision;

        public SoftRangeAttribute(float min, float max, int precision = -1)
        {
            Min = min;
            Max = max;
            Precision = precision;
        }
    }
}
