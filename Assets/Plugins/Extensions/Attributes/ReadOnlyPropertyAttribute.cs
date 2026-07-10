using System;

namespace Extensions.Attributes
{
    /// <summary>
    /// Вывод нередактируемого значения свойства в инспекторе
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public sealed class ReadOnlyPropertyAttribute : Attribute
    {
        public readonly string Label;

        public ReadOnlyPropertyAttribute(string label = null)
        {
            Label = label;
        }
    }
}
