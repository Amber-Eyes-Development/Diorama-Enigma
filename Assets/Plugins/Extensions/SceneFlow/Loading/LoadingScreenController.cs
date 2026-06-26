using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace Extensions.SceneFlow
{
    /// <summary>
    /// Контроллер экрана загрузки: проигрывает ссылочные <see cref="DOTweenAnimation"/> на появление/исчезновение
    /// </summary>
    /// <remarks>
    /// Висит на окне загрузки (его динамически поднимает контроллер окон), публикует себя в канал
    /// и по запросу контроллера сцен играет заданные анимации, сообщая об их завершении колбэком.
    /// Каждая анимация опциональна: пустая ссылка трактуется как мгновенное завершение
    /// </remarks>
    public sealed class LoadingScreenController : MonoBehaviour, ILoadingScreen
    {
        #region Параметры

        [Tooltip("Опционально. Канал публикации экрана для контроллера сцен. Без него экран не виден контроллеру")]
        [SerializeField] private LoadingScreenReference channel;

        [Header("Анимации"), Space]
        [Tooltip("Опционально. Анимация появления экрана (пусто — мгновенно)")]
        [SerializeField] private DOTweenAnimation introAnimation;
        [Tooltip("Опционально. Анимация исчезновения экрана (пусто — мгновенно)")]
        [SerializeField] private DOTweenAnimation outroAnimation;

        #endregion

        #region Внутренние переменные

        private Coroutine playRoutine;

        #endregion

        #region MonoBehaviour

        private void OnEnable()
        {
            if (channel != null)
                channel.Set(this);
        }

        private void OnDisable()
        {
            StopActiveRoutine();

            if (channel != null && ReferenceEquals(channel.Current, this))
                channel.Clear();
        }

        #endregion

        #region ILoadingScreen

        /// <inheritdoc/>
        public void PlayIntro(Action onComplete) => Play(introAnimation, onComplete);

        /// <inheritdoc/>
        public void PlayOutro(Action onComplete) => Play(outroAnimation, onComplete);

        #endregion

        #region Внутренние операции

        private void Play(DOTweenAnimation animation, Action onComplete)
        {
            StopActiveRoutine();

            if (!isActiveAndEnabled || animation == null)
            {
                onComplete?.Invoke();
                return;
            }

            playRoutine = StartCoroutine(PlayRoutine(animation, onComplete));
        }

        private IEnumerator PlayRoutine(DOTweenAnimation animation, Action onComplete)
        {
            EnsureTween(animation);
            animation.DORestart();

            Tween tween = animation.tween;
            while (tween != null && tween.IsActive() && !tween.IsComplete())
                yield return null;

            playRoutine = null;
            onComplete?.Invoke();
        }

        private void StopActiveRoutine()
        {
            if (playRoutine == null)
                return;

            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        /// <summary> Создать твин, если его ещё нет (контроллер сам управляет проигрыванием) </summary>
        private static void EnsureTween(DOTweenAnimation animation)
        {
            if (animation.tween == null)
                animation.CreateTween(regenerateIfExists: false, andPlay: false);
        }

        #endregion
    }
}
