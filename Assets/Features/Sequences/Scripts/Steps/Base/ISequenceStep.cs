using System;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Контракт шага последовательности: единый признак завершённости независимо от реализации
    /// </summary>
    public interface ISequenceStep
    {
        /// <summary> Изменение признака завершённости </summary>
        event Action<bool> onCompletionChanged;

        /// <summary> Метка шага </summary>
        string StepLabel { get; }
        /// <summary> Завершён ли шаг </summary>
        bool IsCompleted { get; }

        /// <summary> Активировать/деактивировать шаг (раннер открывает изменение состояния) </summary>
        /// <param name="active">true — изменение состояния разрешено</param>
        void SetActive(bool active);
        /// <summary> Сбросить состояние шага к исходному </summary>
        void ResetState();
    }
}
