namespace Extensions.Billboard
{
    /// <summary>
    /// Режим поворота билборда
    /// </summary>
    public enum BillboardMode
    {
        /// <summary>
        /// Полный поворот — объект смотрит прямо в камеру (сферический билборд)
        /// </summary>
        FullFaceCamera,
        /// <summary>
        /// Только по оси Y — объект поворачивается горизонтально (цилиндрический билборд)
        /// </summary>
        YAxisOnly
    }
}