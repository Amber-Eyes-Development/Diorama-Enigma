using System.Collections.Generic;
using DG.Tweening;
using Extensions.Log;
using UnityEngine;

namespace Extensions.AnimationSequencer
{
    /// <summary>
    /// Контроллер очерёдности анимаций DOTweenAnimation
    /// </summary>
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
            {
                if (entry.Animation != null)
                    entry.Animation.DOKill();
            }
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

            foreach (var entry in group)
            {
                // Создать твин, прыгнуть в конец и проиграть назад
                entry.Animation.RecreateTweenAndPlay();
                entry.Animation.DOComplete();
                entry.Animation.DOPlayBackwards();
            }

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
                if (toEnd) entry.Animation.DOComplete();
                else entry.Animation.DORewind();
            }
        }

        private static float GetGroupDuration(List<SequenceEntry> group)
        {
            float max = 0f;
            foreach (var entry in group)
            {
                float d = entry.Animation.duration + Mathf.Max(0f, entry.Animation.delay);
                if (d > max) max = d;
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
