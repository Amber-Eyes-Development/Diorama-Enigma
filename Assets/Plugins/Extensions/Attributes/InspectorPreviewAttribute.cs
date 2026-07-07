using System;
using System.Diagnostics;
using UnityEngine;

namespace Extensions.Attributes
{
    /// <summary>
    /// Вывод поля в режиме просмотра без редактирования
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    [Conditional("UNITY_EDITOR")]
    public sealed class InspectorPreviewAttribute : PropertyAttribute { }
}