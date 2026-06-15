using System.Collections.Generic;
using DG.Tweening;
using Extensions.Log;
using UnityEngine;

namespace Extensions.AnimationSequencer
{
    /// <summary>
    /// Собирает анимации DOTweenAnimation в единый Sequence (Append — следом, Join — параллельно)
    /// и управляет им как одним твином
    /// </summary>
    // Раньше DOTweenAnimation (порядок по умолчанию 0): успеть снять autoPlay до того, как тот создаст твин в своём Awake
    [DefaultExecutionOrder(-100)]
    public sealed class AnimationSequencer : MonoBehaviour
    {
        [Header("Настройки"), Space]
        [Tooltip("Автоматически запустить последовательность при старте")]
        [SerializeField] private bool playOnStart = true;

        [Header("Шаги"), Space]
        [SerializeField] private List<SequenceEntry> entries = new();

        private Sequence sequence;

        #region Unity Lifecycle

        private void Awake()
        {
            foreach (var entry in entries)
            {
                if (entry.Animation == null) continue;

                entry.Animation.autoPlay = false;
                entry.Animation.autoKill = false;
            }
        }

        private void Start()
        {
            if (playOnStart) Play();
        }

        private void OnDisable() => Stop();

        private void OnDestroy() => sequence?.Kill();

        #endregion

        #region Управление последовательностью

        /// <summary> Запустить последовательность с начала </summary>
        public void Play()
        {
            if (!EnsureSequence()) return;

            sequence.Restart();
        }

        /// <summary> Проиграть последовательность в обратную сторону (с конца к началу) </summary>
        public void PlayBackwards()
        {
            if (!EnsureSequence()) return;

            sequence.PlayBackwards();
        }

        /// <summary> Отключить авто-запуск при старте (для внешнего распорядителя проигрывания) </summary>
        public void DisableAutoPlay() => playOnStart = false;

        /// <summary> Мгновенно установить последовательность в конечное состояние (без проигрыша) </summary>
        public void SetAtEnd()
        {
            if (EnsureSequence()) sequence.Complete();
        }

        /// <summary> Мгновенно установить последовательность в начальное состояние (без проигрыша) </summary>
        public void SetAtStart()
        {
            if (EnsureSequence()) sequence.Rewind();
        }

        /// <summary> Остановить (поставить на паузу) последовательность </summary>
        public void Stop() => sequence?.Pause();

        #endregion

        #region Internal

        private bool EnsureSequence()
        {
            // Пересборка пересоздала бы относительные твины против уже повёрнутого объекта и копила бы поворот
            if (sequence != null && sequence.IsActive()) return true;

            sequence = BuildSequence();
            return sequence != null;
        }

        private Sequence BuildSequence()
        {
            Sequence built = DOTween.Sequence().SetAutoKill(false).Pause();
            bool hasAny = false;

            float groupStart = 0f;
            float timelineEnd = 0f;

            foreach (var entry in entries)
            {
                if (entry.Animation == null)
                {
                    ServiceDebug.LogWarning($"{name}: шаг без ссылки на DOTweenAnimation пропущен");
                    continue;
                }

                if (!hasAny || !entry.JoinWithPrevious) groupStart = timelineEnd;

                float delay = Mathf.Max(0f, entry.Animation.delay);
                Tween tween = CreateDelayFreeTween(entry.Animation);
                if (tween == null) continue;

                float start = groupStart + delay;
                built.Insert(start, tween);

                float span = entry.Animation.duration * Mathf.Max(1, entry.Animation.loops);
                timelineEnd = Mathf.Max(timelineEnd, start + span);
                hasAny = true;
            }

            if (hasAny) return built;

            built.Kill();
            return null;
        }

        /// <summary>
        /// Собрать твин без собственной задержки: её закладываем в позицию вставки (Insert),
        /// иначе при обратном проигрывании задержка DOTween не отражается зеркально
        /// </summary>
        private static Tween CreateDelayFreeTween(DOTweenAnimation animation)
        {
            float delay = animation.delay;

            animation.delay = 0f;
            animation.CreateTween(regenerateIfExists: true, andPlay: false);
            animation.delay = delay;

            return animation.tween;
        }

        #endregion
    }
}
