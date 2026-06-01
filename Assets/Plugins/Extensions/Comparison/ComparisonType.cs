using UnityEngine;

namespace Extensions.Types
{
    /// <summary>
    /// Тип сравнения
    /// </summary>
    public enum ComparisonType
    {
        /// <summary>
        /// &gt;
        /// </summary>
        [InspectorName(">")] Greater = 0,
        /// <summary>
        /// &gt;=
        /// </summary>
        [InspectorName(">=")] GreaterOrEqual = 1,
        /// <summary>
        /// &lt;
        /// </summary>
        [InspectorName("<")] Less = 2,
        /// <summary>
        /// &lt;=
        /// </summary>
        [InspectorName("<=")] LessOrEqual = 3,
        /// <summary>
        /// =
        /// </summary>
        [InspectorName("=")] Equal = 4,
        /// <summary>
        /// !=
        /// </summary>
        [InspectorName("!=")] NotEqual = 5
    }
}