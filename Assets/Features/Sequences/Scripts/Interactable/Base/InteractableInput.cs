using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// База компонента ввода: общий источник состояния (шаг <see cref="SequenceStep"/>)
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(StepReference))]
    public abstract class InteractableInput : MonoBehaviour
    {
        protected SequenceStep State { get; private set; }

        /// <summary> Доступно ли взаимодействие прямо сейчас: шаг есть, разблокирован, не на кулдауне и (если необратим) ещё не завершён </summary>
        protected bool CanInteract => State != null
            && State.IsUnlocked
            && !State.IsOnCooldown
            && (!State.Irreversible || !State.IsCompleted);

        protected virtual void Awake() => State = GetComponent<StepReference>().Step as SequenceStep;

        /// <summary> Сообщить шагу об отклонённой попытке изменить его состояние — для фидбэка вьюшек </summary>
        protected void ReportRejectedInteraction() => State?.NotifyInteractionRejected();
    }
}
