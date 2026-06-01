using System.Collections;
using UnityEngine;

namespace Extensions.Coroutines
{
    /// <summary>
    /// Базовая модель управляемой корутиной
    /// </summary>
    public class CoroutineTask
    {
        protected readonly MonoBehaviour owner;
        protected Coroutine coroutine;

        /// <summary>
        /// Статус активности корутины
        /// </summary>
        public bool IsRunning => coroutine != null;

        /// <summary>
        /// Конструктор модели управления корутиной
        /// </summary>
        /// <param name="owner">Скрипт-хозяин корутины</param>
        public CoroutineTask(MonoBehaviour owner) => this.owner = owner;

        /// <summary>
        /// Запуск новой корутины
        /// </summary>
        /// <param name="routine">Корутина</param>
        public void Start(IEnumerator routine)
        {
            if (routine == null)
                return;

            if (!IsOwnerValidToStart())
                return;

            Stop();

            coroutine = owner.StartCoroutine(Run(routine));
        }

        /// <summary>
        /// Остановка текущей корутины
        /// </summary>
        public void Stop()
        {
            if (coroutine == null)
                return;

            if (owner != null)
                owner.StopCoroutine(coroutine);

            coroutine = null;
        }

        protected virtual IEnumerator Run(IEnumerator routine)
        {
            yield return routine;
            coroutine = null;
        }

        protected bool IsOwnerValidToStart() => owner != null && owner.gameObject.activeInHierarchy;
    }
}