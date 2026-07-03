namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Слушатель привязки кнопки блока: получает блок при (пере)построении кнопки.
    /// Точка расширения кнопки без обратной зависимости на другие фичи (напр. рекорд из статистики)
    /// </summary>
    public interface IDioramaButtonBindListener
    {
        /// <summary> Кнопка привязана к блоку </summary>
        /// <param name="block">Блок</param>
        void OnBound(DioramaBlock block);
    }
}
