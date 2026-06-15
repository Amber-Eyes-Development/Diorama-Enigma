using UnityEngine;

namespace Extensions.Attributes
{
    /// <summary>
    /// Вывод элементов enum в определенном диапазоне
    /// </summary>
    public sealed class EnumRangeAttribute : PropertyAttribute
    {
        public readonly int Min;
        public readonly int Max;

        public EnumRangeAttribute(int min, int max)
        {
            Min = min;
            Max = max;
        }
    }
}