using DioramaEnigma.Scratch;
using Extensions.Log;
using UnityEngine;

namespace DioramaEnigma.Completers
{
    /// <summary>
    /// Завершитель: засчитывает шаг по проценту стирания поверхности <see cref="ScratchSurface"/>
    /// </summary>
    public sealed class ScratchCompleter : AbstractCompleter
    {
        [Tooltip("Стираемая поверхность — источник прогресса")]
        [SerializeField] private ScratchSurface surface;
        [Tooltip("Доля стирания, при которой шаг засчитывается")]
        [Range(0f, 1f)]
        [SerializeField] private float threshold = 0.85f;
        [Tooltip("Дочистить поверхность полностью при зачёте (иначе остаток можно дотирать вручную)")]
        [SerializeField] private bool fillOnComplete = true;

        protected override void Awake()
        {
            base.Awake();

            if (surface == null)
                ServiceDebug.LogError(this, $"Не назначена {nameof(surface)} — нечего засчитывать");
        }

        private void OnEnable()
        {
            if (surface == null) return;

            surface.onProgressChanged += OnProgressChanged;

            if (State != null && State.IsCompleted && fillOnComplete) surface.SetErased(true);
            Evaluate(surface.Progress);
        }

        private void OnDisable()
        {
            if (surface != null) surface.onProgressChanged -= OnProgressChanged;
        }

        #region Internal

        private void OnProgressChanged(float progress) => Evaluate(progress);

        private void Evaluate(float progress)
        {
            if (State == null) return;
            if (progress < threshold || State.IsCompleted) return;

            State.ForceValue(State.CompletionState);
            if (fillOnComplete) surface.SetErased(true);
        }

        #endregion
    }
}
