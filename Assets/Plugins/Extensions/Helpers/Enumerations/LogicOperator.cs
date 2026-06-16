namespace Extensions.Helpers.Enumerations
{
    /// <summary>
    /// Логический оператор объединения булевых операндов
    /// </summary>
    public enum LogicOperator
    {
        /// <summary> И — истинны все операнды </summary>
        And = 0,
        /// <summary> ИЛИ — истинен хотя бы один операнд </summary>
        Or = 1,
        /// <summary> НЕ — ни один операнд не истинен </summary>
        Not = 2,
    }
}
