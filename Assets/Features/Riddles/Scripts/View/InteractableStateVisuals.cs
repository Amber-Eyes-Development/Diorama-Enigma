using System;
using DG.Tweening;
using Extensions.AnimationSequencer;
using Extensions.Log;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Визуальная реакция на смену стейта объекта
    /// </summary>
    public sealed class InteractableStateVisuals : InteractableViewBehaviour
    {
        /// <summary> Визуал для одного стейта </summary>
        [Serializable]
        public struct StateConfig
        {
            public int StateIndex;
            public GameObject[] ObjectsToEnable;
            public GameObject[] ObjectsToDisable;
            public DOTweenAnimation Animation;
            public AnimationSequencer Sequencer;
        }

        [SerializeField] private StateConfig[] stateConfigs;

        /// <summary> Применить визуал состояния с анимациями </summary>
        public void Apply(int stateIndex) => ApplyInternal(stateIndex, silent: false);

        /// <summary> Применить визуал состояния без анимаций (восстановление после загрузки) </summary>
        public void ApplySilent(int stateIndex) => ApplyInternal(stateIndex, silent: true);

        protected override void Subscribe()
        {
            Interactable.State.onStateChanged += OnStateChanged;

            if (Interactable.State.Current != 0)
                ApplySilent(Interactable.State.Current);
        }

        protected override void Unsubscribe() => Interactable.State.onStateChanged -= OnStateChanged;

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
                if (silent) config.Animation.DOComplete();
            }

            if (!silent && config.Sequencer != null)
                config.Sequencer.Play();
        }

        #endregion
    }
}
