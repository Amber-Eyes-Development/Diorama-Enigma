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
