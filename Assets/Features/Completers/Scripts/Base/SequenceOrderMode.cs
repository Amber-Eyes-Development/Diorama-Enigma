namespace DioramaEnigma.Completers
{
    /// <summary>
    /// Реакция <see cref="OrderedSequenceCompleter"/> на выполнение шагов не по очереди
    /// </summary>
    public enum SequenceOrderMode
    {
        /// <summary> Шаг, завершённый не в свою очередь, сразу откатывается обратно </summary>
        RevertOutOfOrder,
        /// <summary> Шаги можно выполнять в любом порядке; когда выполнены все — если порядок был неверным, все откатываются </summary>
        CheckOnComplete,
        /// <summary> Шаг, завершённый раньше своей очереди, засчитывается автоматически, когда очередь до него дойдёт </summary>
        AutoCascade,
    }
}
