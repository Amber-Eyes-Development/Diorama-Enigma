using DioramaEnigma.Sequences;
using UnityEngine;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Рантайм-обёртка инстанса диорамы
    /// </summary>
    /// <remarks>
    /// Связывает раннер <see cref="SequenceRunner"/> с сервисом доступа <see cref="DioramaAccessService"/>
    /// и гейтит ввод по фокусу. Неактивные диорамы остаются загруженными и работающими, но не принимают ввод.
    /// Автоматически добавляется спавнером <see cref="DioramaSpawner"/> на корень инстанса
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class DioramaInstance : MonoBehaviour
    {
        /// <summary> Определение диорамы </summary>
        public DioramaDefinition Definition => definition;
        /// <summary> Раннер диорамы </summary>
        public SequenceRunner Runner => runner;

        private DioramaDefinition definition;
        private DioramaAccessService access;
        private DioramaSpawner spawner;
        private SequenceRunner runner;
        private InteractableInput[] inputs;

        /// <summary> Привязать инстанс к определению, сервису доступа, раннеру и спавнеру </summary>
        public void Bind(DioramaDefinition definition, DioramaAccessService access, SequenceRunner runner, DioramaSpawner spawner)
        {
            this.definition = definition;
            this.access = access;
            this.runner = runner;
            this.spawner = spawner;

            inputs = GetComponentsInChildren<InteractableInput>(true);
            runner.onSequenceCompleted += OnSequenceCompleted;

            if (definition.IsCompleted) access.MarkCompleted(definition);
        }

        /// <summary> Включить/выключить взаимодействие (фокус) </summary>
        public void SetFocused(bool focused)
        {
            if (inputs == null) return;

            foreach (var input in inputs)
                if (input != null) input.enabled = focused;
        }

        /// <summary> Перейти к связанной диораме, если она открыта </summary>
        /// <param name="target"> Целевая диорама </param>
        /// <returns> true, если переход выполнен </returns>
        public bool TryFocus(DioramaDefinition target)
        {
            if (target == null || access == null || spawner == null) return false;
            if (!access.IsUnlocked(target)) return false;

            spawner.FocusDiorama(target);
            return true;
        }

        private void OnSequenceCompleted() => access?.MarkCompleted(definition);

        private void OnDestroy()
        {
            if (runner != null) runner.onSequenceCompleted -= OnSequenceCompleted;
        }
    }
}
