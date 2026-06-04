namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Биндинг «условие → реакция»
    /// </summary>
    public interface IReactionBinding
    {
        /// <summary> Условие срабатывания </summary>
        ReactionTrigger ReactionTrigger { get; }
    }
}
