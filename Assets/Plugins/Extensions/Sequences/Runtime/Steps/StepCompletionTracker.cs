using System.Collections.Generic;
using Extensions.Helpers;
using Extensions.Reactive;

namespace Extensions.Sequences
{
    /// <summary>
    /// Рантайм-логика разблокировки шага: активность + гейты + латч необратимости.
    /// Завершённость хранит сам шаг и сообщает её сюда через <see cref="SetCompleted"/>.
    /// </summary>
    public sealed class StepCompletionTracker
    {
        /// <summary> Разблокировано ли изменение состояния прямо сейчас </summary>
        public ReadOnlyReactiveProperty<bool> Unlocked => unlocked.AsReadOnly();
        /// <summary> Текущее значение разблокировки </summary>
        public bool IsUnlocked => unlocked.Value;
        /// <summary> Активна ли группа шага (открыта движком, безотносительно гейтов/латча) </summary>
        public bool IsActive => isActive;

        private readonly ReactiveProperty<bool> unlocked = new(false);
        private readonly List<ActionDisposable> gateSubs = new();

        private IReadOnlyList<Gate> gates;
        private bool isActive;
        private bool irreversible;
        private bool completed;

        /// <summary> Инициализировать исходную завершённость/необратимость (гейты приходят при активации) </summary>
        public void Initialize(bool completed, bool irreversible)
        {
            ClearGateSubs();
            gates = null;
            isActive = false;
            this.irreversible = irreversible;
            this.completed = completed;
            unlocked.SetValue(Compute());
        }

        /// <summary> Активировать/деактивировать. При активации принимает гейты записи. Возвращает true, если активность изменилась </summary>
        public bool SetActive(bool active, IReadOnlyList<Gate> gates)
        {
            bool changed = isActive != active;
            if (changed)
            {
                if (active)
                {
                    this.gates = gates;
                    isActive = true;
                    ObserveGates(true);
                }
                else
                {
                    ObserveGates(false);
                    isActive = false;
                    this.gates = null;
                }
            }

            unlocked.SetValue(Compute());
            return changed;
        }

        /// <summary> Сообщить новую завершённость (влияет на латч необратимости) </summary>
        public void SetCompleted(bool completed)
        {
            this.completed = completed;
            unlocked.SetValue(Compute());
        }

        #region Internal

        private bool Compute() => isActive && AllGatesSatisfied() && !(irreversible && completed);

        private bool AllGatesSatisfied()
        {
            if (gates == null) return true;

            foreach (var gate in gates)
                if (gate != null && !gate.IsSatisfied())
                    return false;

            return true;
        }

        private void ObserveGates(bool observe)
        {
            if (observe)
            {
                if (gates == null) return;

                foreach (var gate in gates)
                {
                    if (gate == null) continue;
                    gate.StartObserving();
                    gateSubs.Add(gate.SatisfactionChanged.Subscribe(OnGateChanged));
                }
            }
            else
            {
                ClearGateSubs();

                if (gates == null) return;
                foreach (var gate in gates)
                    gate?.StopObserving();
            }
        }

        private void ClearGateSubs()
        {
            foreach (var sub in gateSubs) sub.Dispose();
            gateSubs.Clear();
        }

        private void OnGateChanged() => unlocked.SetValue(Compute());

        #endregion
    }
}
