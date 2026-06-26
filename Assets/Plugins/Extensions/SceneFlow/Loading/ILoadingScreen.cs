using System;

namespace Extensions.SceneFlow
{
    /// <summary>
    /// Экран загрузки
    /// </summary>
    /// <remarks>
    /// Реализуется контроллером окна загрузки. Контроллер сцен дёргает анимации появления/исчезновения
    /// и ждёт их завершения через колбэк, не зная о конкретной реализации (DOTween, шейдер и т.п.)
    /// </remarks>
    public interface ILoadingScreen
    {
        /// <summary>
        /// Проиграть появление экрана; вызвать <paramref name="onComplete"/> по завершении
        /// </summary>
        void PlayIntro(Action onComplete);

        /// <summary>
        /// Проиграть исчезновение экрана; вызвать <paramref name="onComplete"/> по завершении
        /// </summary>
        void PlayOutro(Action onComplete);
    }
}
