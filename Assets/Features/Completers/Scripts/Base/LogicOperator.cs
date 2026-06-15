namespace DioramaEnigma.Completers
{
    /// <summary>
    /// Способ объединения операндов <see cref="LogicCompleter"/>
    /// </summary>
    public enum LogicOperator
    {
        /// <summary> Истинны все операнды </summary>
        And,
        /// <summary> Истинен хотя бы один операнд </summary>
        Or,
    }
}
