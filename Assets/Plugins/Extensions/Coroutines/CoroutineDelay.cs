using System;
using System.Collections;
using Extensions.Log;
using UnityEngine;

namespace Extensions.Coroutines
{
    /// <summary>
    /// Stateless-утилита для отложенного выполнения действия через корутину
    /// </summary>
    public static class CoroutineDelay
    {
        /// <summary>
        /// Запуск действия с задержкой в секундах перед исполнением
        /// </summary>
        /// <param name="owner">Хозяин корутины</param>
        /// <param name="delay">Задержка в секундах</param>
        /// <param name="action">Действие</param>
        public static void Run(MonoBehaviour owner, float delay, Action action)
        {
            if (owner == null || !owner.isActiveAndEnabled)
                return;

            if (delay < 0f) delay = 0f;
            
            if (action == null)
            {
                ServiceDebug.LogError("Действие не определено, задержка не выполнена");
                return;
            }

            owner.StartCoroutine(Routine(owner, delay, action));
        }
        
        /// <summary>
        /// Запуск действия с задержкой в кадрах перед исполнением
        /// </summary>
        /// <param name="owner">Хозяин корутины</param>
        /// <param name="frames">Задержка в кадрах</param>
        /// <param name="action">Действие</param>
        public static void Run(MonoBehaviour owner, int frames, Action action)
        {
            if (owner == null || !owner.isActiveAndEnabled)
                return;

            if (frames < 0) frames = 0;
            
            if (action == null)
            {
                ServiceDebug.LogError("Действие не определено, задержка не выполнена");
                return;
            }

            owner.StartCoroutine(Routine(owner, frames, action));
        }
        
        /// <summary>
        /// Запуск действия с задержкой в 1 кадр перед исполнением
        /// </summary>
        /// <param name="owner">Хозяин корутины</param>
        /// <param name="action">Действие</param>
        public static void Run(MonoBehaviour owner, Action action) => Run(owner, 1, action);

        private static IEnumerator Routine(MonoBehaviour owner, float delay, Action action)
        {
            yield return new WaitForSeconds(delay);

            if (owner == null || !owner.isActiveAndEnabled)
                yield break;

            action?.Invoke();
        }
        
        private static IEnumerator Routine(MonoBehaviour owner, int frames, Action action)
        {
            for (int i = 0; i < frames; i++)
                yield return null;

            if (owner == null || !owner.isActiveAndEnabled)
                yield break;

            action?.Invoke();
        }
    }
}