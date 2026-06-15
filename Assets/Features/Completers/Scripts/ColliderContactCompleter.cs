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
            if (State == null) return;

            var reference = other.GetComponentInParent<StepReference>();
            if (reference == null || reference.Step != State) return;

            State.SetValue(State.CompletionState);
        }
    }
}