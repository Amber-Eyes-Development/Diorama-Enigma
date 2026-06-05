using System;
using Extensions.Coroutines;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// База компонента ввода: применяет изменение значения с опциональной задержкой,
    /// блокируя повторный ввод на время задержки. Корректность «не раньше времени»
    /// обеспечивает veto самого шага (<see cref="StepCompletionTracker"/>), а не блокировка ввода.
    /// </summary>
    public abstract class InteractableInput : MonoBehaviour
    {
        [Header("Задержка"), Space]
        [Tooltip("Задержка применения изменения, сек. Во время задержки ввод игнорируется")]
        [Min(0f)]
        [SerializeField] private float delay;

        private bool busy;

        /// <summary> Занят ли компонент задержкой (ввод игнорируется) </summary>
        protected bool IsBusy => busy;

        protected virtual void OnEnable() => busy = false;

        /// <summary> Применить изменение с учётом задержки и блокировки ввода на её время </summary>
        /// <param name="change">Изменение состояния</param>
        protected void Apply(Action change)
        {
            if (busy || change == null) return;

            if (delay <= 0f)
            {
                change();
                return;
            }

            busy = true;
            CoroutineDelay.Run(this, delay, () =>
            {
                busy = false;
                change();
            });
        }
    }
}
