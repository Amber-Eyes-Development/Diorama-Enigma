using System.Collections.Generic;
using DG.Tweening;
using Extensions.Log;
using UnityEngine;

namespace Extensions.AnimationSequencer
{
    /// <summary>
    /// Контроллер очерёдности анимаций DOTweenAnimation
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

        private Tween pendingCall;
        private List<List<SequenceEntry>> groups;

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

        #endregion

        #region Управление последовательностью

        /// <summary> Запустить последовательность с начала </summary>
        public void Play()
        {
            Stop();

            groups = BuildGroups();
            if (groups.Count == 0) return;

            PlayGroupAt(0);
        }

        /// <summary> Проиграть последовательность в обратном порядке (анимации — назад) </summary>
        public void PlayBackwards()
        {
            Stop();

            groups = BuildGroups();
            if (groups.Count == 0) return;

            PlayGroupBackwardsAt(groups.Count - 1);
        }

        /// <summary> Отключить авто-запуск при старте (для внешнего распорядителя проигрывания) </summary>
        public void DisableAutoPlay() => playOnStart = false;

        /// <summary> Мгновенно установить последовательность в конечное состояние (без проигрыша) </summary>
        public void SetAtEnd() => SetAll(toEnd: true);

        /// <summary> Мгновенно установить последовательность в начальное состояние (без проигрыша) </summary>
        public void SetAtStart() => SetAll(toEnd: false);

        /// <summary> Остановить последовательность и все активные анимации </summary>
        public void Stop()
        {
            pendingCall?.Kill();
            pendingCall = null;

            foreach (var entry in entries)
                entry.Animation?.tween?.Pause();
        }

        #endregion

        #region Internal

        private void PlayGroupAt(int index)
        {
            if (index >= groups.Count) return;

            var group = groups[index];

            foreach (var entry in group)
                entry.Animation.RecreateTweenAndPlay();

            bool hasNext = index < groups.Count - 1;
            if (!hasNext) return;

            float duration = GetGroupDuration(group);
            pendingCall = DOVirtual.DelayedCall(duration, () => PlayGroupAt(index + 1));
        }

        private void PlayGroupBackwardsAt(int index)
        {
            if (index < 0) return;

            var group = groups[index];
            group.Reverse();

            foreach (var entry in group)
                entry.Animation.tween?.PlayBackwards();

            if (index == 0) return;

            float duration = GetGroupDuration(group);
            pendingCall = DOVirtual.DelayedCall(duration, () => PlayGroupBackwardsAt(index - 1));
        }

        private void SetAll(bool toEnd)
        {
            Stop();

            foreach (var entry in entries)
            {
                if (entry.Animation == null) continue;

                entry.Animation.RecreateTweenAndPlay();
                var tween = entry.Animation.tween;
                if (tween == null) continue;

                tween.Complete();
                if (!toEnd) tween.Rewind();
            }
        }

        private static float GetGroupDuration(List<SequenceEntry> group)
        {
            float max = 0f;
            foreach (var entry in group)
            {
                int loops = entry.Animation.loops;
                float total = entry.Animation.duration * (loops > 0 ? loops : 1) + Mathf.Max(0f, entry.Animation.delay);
                if (total > max) max = total;
            }
            return max;
        }

        private List<List<SequenceEntry>> BuildGroups()
        {
            var result = new List<List<SequenceEntry>>();
            List<SequenceEntry> current = null;

            foreach (var entry in entries)
            {
                if (entry.Animation == null)
                {
                    ServiceDebug.LogWarning($"{name}: шаг без ссылки на DOTweenAnimation пропущен");
                    continue;
                }

                if (!entry.JoinWithPrevious || current == null)
                {
                    current = new List<SequenceEntry>();
                    result.Add(current);
                }

                current.Add(entry);
            }

            return result;
        }

        #endregion
    }
}
