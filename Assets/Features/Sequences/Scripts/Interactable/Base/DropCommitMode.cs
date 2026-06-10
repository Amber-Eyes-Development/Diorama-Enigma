namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Когда завершается шаг при попадании перетаскиваемого объекта в зону дропа
    /// </summary>
    public enum DropCommitMode
    {
        /// <summary> На отпускании мыши над зоной </summary>
        OnRelease = 0,
        /// <summary> Моментально при попадании в зону (не дожидаясь отпускания) </summary>
        OnEnter = 1,
    }
}
