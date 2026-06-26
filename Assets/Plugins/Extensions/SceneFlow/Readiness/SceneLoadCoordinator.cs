using System.Collections;
using System.Collections.Generic;
using Extensions.Log;
using UnityEngine;

namespace Extensions.SceneFlow
{
    /// <summary>
    /// Координатор готовности целевой сцены
    /// </summary>
    /// <remarks>
    /// Лежит в корне игровой сцены. Системы регистрируют свои <see cref="ISceneLoadStep"/>,
    /// а бутстрап закрывает приём шагов через <see cref="SealRegistration"/>. Публикует себя
    /// в канал, по которому контроллер сцен ждёт реальную готовность сцены и тянет прогресс.
    /// Сцена без координатора грузится без ожидания готовности
    /// </remarks>
    public sealed class SceneLoadCoordinator : MonoBehaviour
    {
        #region Параметры

        [Tooltip("Опционально. Канал публикации координатора для контроллера сцен. Без него готовность сцены не отслеживается")]
        [SerializeField] private SceneLoadCoordinatorReference channel;

        [Tooltip("Опционально. Закрыть приём шагов автоматически в конце первого кадра, если бутстрап не зовёт SealRegistration вручную")]
        [SerializeField] private bool sealAtEndOfFirstFrame;

        #endregion

        #region Свойства

        /// <summary>
        /// Приём шагов закрыт и все шаги завершены
        /// </summary>
        public bool IsComplete => isRegistrationSealed && AreAllStepsDone();

        /// <summary>
        /// Усреднённый прогресс шагов от 0 до 1 (до закрытия приёма не достигает единицы)
        /// </summary>
        public float AggregateProgress
        {
            get
            {
                if (steps.Count == 0)
                    return isRegistrationSealed ? 1f : 0f;

                float sum = 0f;
                foreach (ISceneLoadStep step in steps)
                    sum += step == null ? 0f : Mathf.Clamp01(step.Progress);

                float average = sum / steps.Count;
                return isRegistrationSealed ? average : Mathf.Min(average, ProgressCapBeforeSeal);
            }
        }

        #endregion

        #region Внутренние переменные

        private const float ProgressCapBeforeSeal = 0.99f;

        private readonly List<ISceneLoadStep> steps = new();
        private bool isRegistrationSealed;

        #endregion

        #region MonoBehaviour

        private void OnEnable()
        {
            if (channel != null)
                channel.Set(this);
        }

        private void OnDisable()
        {
            if (channel != null && ReferenceEquals(channel.Current, this))
                channel.Clear();
        }

        private void Start()
        {
            if (sealAtEndOfFirstFrame)
                StartCoroutine(SealAtEndOfFrameRoutine());
        }

        #endregion

        #region Публичный API

        /// <summary>
        /// Зарегистрировать шаг загрузки
        /// </summary>
        /// <param name="step">Шаг</param>
        public void Register(ISceneLoadStep step)
        {
            if (step == null)
                return;

            if (isRegistrationSealed)
            {
                ServiceDebug.LogWarning($"Регистрация шага после {nameof(SealRegistration)} проигнорирована");
                return;
            }

            if (steps.Contains(step))
                return;

            steps.Add(step);
        }

        /// <summary>
        /// Закрыть приём шагов: все ожидаемые шаги заявлены
        /// </summary>
        public void SealRegistration() => isRegistrationSealed = true;

        #endregion

        #region Внутренние операции

        private bool AreAllStepsDone()
        {
            foreach (ISceneLoadStep step in steps)
            {
                if (step == null)
                    continue;

                if (!step.IsDone)
                    return false;
            }

            return true;
        }

        private IEnumerator SealAtEndOfFrameRoutine()
        {
            yield return null;

            if (!isRegistrationSealed)
                SealRegistration();
        }

        #endregion
    }
}
