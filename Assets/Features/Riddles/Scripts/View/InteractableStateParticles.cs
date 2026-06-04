using System;
using Extensions.Log;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Реакция системой частиц на смену стейта объекта
    /// </summary>
    public sealed class InteractableStateParticles : InteractableViewBehaviour
    {
        /// <summary> Действия с частицами для одного стейта </summary>
        [Serializable]
        public struct StateParticles
        {
            public int StateIndex;
            public ParticleSystemAction[] Actions;
        }

        [SerializeField] private StateParticles[] stateParticles;

        /// <summary> Применить частицы состояния </summary>
        public void Apply(int stateIndex) => ApplyInternal(stateIndex, silent: false);

        /// <summary> Применить частицы состояния без всплеска спавна (восстановление после загрузки) </summary>
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
            foreach (var config in stateParticles)
            {
                if (config.StateIndex != stateIndex) continue;

                config.Actions.Apply(silent);
                return;
            }

            if (stateIndex != 0)
                ServiceDebug.LogWarning(this, $"StateParticles для стейта {stateIndex} не найден");
        }

        #endregion
    }
}
