namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Реакция на отпускание перетаскиваемого объекта вне зоны дропа
    /// </summary>
    public enum DragReleaseMode
    {
        /// <summary> Свободно поставить на сцену: упереть коллайдером в первую поверхность вдоль луча камеры </summary>
        PlaceOnScene = 0,
        /// <summary> Вернуть в исходную позицию </summary>
        ReturnToStart = 1,
    }
}
