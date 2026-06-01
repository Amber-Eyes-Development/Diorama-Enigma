using System;
using DG.Tweening;
using Extensions.AnimationSequencer;
using Extensions.Log;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Визуальная реакция на смену стейта <see cref="InteractableObject"/>.
    /// Размещается на том же GameObject. Поддерживает два режима применения:
    /// анимированный (нормальная игра) и тихий (<see cref="ApplySilent"/> —
    /// для восстановления состояния после загрузки без воспроизведения анимаций).
    /// </summary>
    public sealed class InteractableStateVisuals : MonoBehaviour
    {
        /// <summary>Конфигурация визуала для одного стейта</summary>
        [Serializable]
        public struct StateConfig
        {
            [Tooltip("Индекс стейта из InteractableObject")]
            public int StateIndex;

            [Tooltip("Объекты, которые включаются при переходе в этот стейт")]
            public GameObject[] ObjectsToEnable;

            [Tooltip("Объекты, которые выключаются при переходе в этот стейт")]
            public GameObject[] ObjectsToDisable;

            [Tooltip("DOTweenAnimation, воспроизводимый при переходе в этот стейт. " +
                     "При тихом восстановлении — прокручивается к конечному состоянию без анимации.")]
            public DOTweenAnimation Animation;

            [Tooltip("AnimationSequencer, запускаемый при переходе в этот стейт. " +
                     "При тихом восстановлении — не воспроизводится.")]
            public AnimationSequencer Sequencer;
        }

        [SerializeField] private StateConfig[] stateConfigs;

        private InteractableObject interactable;

        #region Unity Lifecycle

        private void Awake()
        {
            interactable = GetComponent<InteractableObject>();

            if (interactable == null)
                ServiceDebug.LogWarning(this, "InteractableObject не найден на этом объекте");
        }

        private void OnEnable()
        {
            if (interactable == null) return;

            interactable.State.onStateChanged += OnStateChanged;

            // Восстановить визуал текущего стейта без анимации —
            // нужно при загрузке сохранения или повторном включении объекта.
            if (interactable.State.Current != 0)
                ApplySilent(interactable.State.Current);
        }

        private void OnDisable()
        {
            if (interactable != null)
                interactable.State.onStateChanged -= OnStateChanged;
        }

        #endregion

        /// <summary>
        /// Применить визуал стейта с анимациями (нормальная игра).
        /// </summary>
        public void Apply(int stateIndex) => ApplyInternal(stateIndex, silent: false);

        /// <summary>
        /// Применить визуал стейта без анимаций.
        /// Включает/выключает объекты немедленно; DOTweenAnimation прокручивается к конечному
        /// состоянию без воспроизведения; AnimationSequencer пропускается.
        /// Вызывается автоматически при загрузке сохранения.
        /// </summary>
        public void ApplySilent(int stateIndex) => ApplyInternal(stateIndex, silent: true);

        #region Internal

        private void OnStateChanged(int stateIndex) => Apply(stateIndex);

        private void ApplyInternal(int stateIndex, bool silent)
        {
            foreach (var config in stateConfigs)
            {
                if (config.StateIndex != stateIndex) continue;

                ApplyConfig(config, silent);
                return;
            }

            // Стейт 0 не требует конфига — объект уже в начальном визуальном состоянии
            if (stateIndex != 0)
                ServiceDebug.LogWarning(this, $"StateConfig для стейта {stateIndex} не найден");
        }

        private void ApplyConfig(StateConfig config, bool silent)
        {
            if (config.ObjectsToEnable != null)
                foreach (var obj in config.ObjectsToEnable)
                    if (obj != null) obj.SetActive(true);

            if (config.ObjectsToDisable != null)
                foreach (var obj in config.ObjectsToDisable)
                    if (obj != null) obj.SetActive(false);

            if (config.Animation != null)
            {
                config.Animation.DORestart();
                // В тихом режиме мгновенно прокручиваем к конечному состоянию
                if (silent) config.Animation.DOComplete();
            }

            if (!silent && config.Sequencer != null)
                config.Sequencer.Play();
        }

        #endregion
    }
}
