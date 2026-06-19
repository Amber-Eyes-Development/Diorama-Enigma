using System;
using System.Diagnostics;
using UnityEngine;

namespace Extensions.Attributes
{
    /// <summary>
    /// Показывает поле в инспекторе только если значение другого поля равно заданному
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    [Conditional("UNITY_EDITOR")]
    public sealed class ShowIfAttribute : PropertyAttribute
    {
        /// <summary> Имя поля-условия (в той же вложенности) </summary>
        public readonly string FieldName;

        /// <summary> Значение поля-условия, при котором поле отображается </summary>
        public readonly object Value;

        public ShowIfAttribute(string fieldName, object value)
        {
            FieldName = fieldName;
            Value = value;
        }
    }
}
