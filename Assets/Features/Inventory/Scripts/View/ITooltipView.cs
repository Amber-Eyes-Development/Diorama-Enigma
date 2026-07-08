namespace DioramaEnigma.Inventory
{
    /// <summary> Поверхность всплывающей плашки с текстом предмета </summary>
    public interface ITooltipView
    {
        /// <summary> Показать плашку с текстом </summary>
        void Show(string text);

        /// <summary> Скрыть плашку </summary>
        void Hide();
    }
}
