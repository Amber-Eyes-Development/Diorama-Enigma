using UnityEngine;
using DioramaEnigma.Sequences;

namespace DioramaEnigma.Completers
{
    /// <summary>
    /// Завершитель: завершает шаг при контакте с коллайдером зоны, чей <see cref="StepReference"/> ссылается на тот же шаг
    /// </summary>
    /// <remarks>
    /// Требуется коллайдер для обоих компонентов, для зоны триггер
    /// </remarks>
    public sealed class ColliderContactCompleter : AbstractCompleter
    {
        private void OnTriggerEnter(Collider other)
        {
            var reference = other.GetComponentInParent<StepReference>();
            SetState(reference, State.CompletionState);
        }
        
        private void OnTriggerExit(Collider other)
        {
            var reference = other.GetComponentInParent<StepReference>();
            SetState(reference, !State.CompletionState);
        }

        private void SetState(StepReference reference, bool state)
        {
            if (State == null) return;
            if (reference == null || reference.Step != State) return;

            State.SetValue(state, notify: false);
        }
    }
}