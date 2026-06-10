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

        public SoftRangeAttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }
    }
}
