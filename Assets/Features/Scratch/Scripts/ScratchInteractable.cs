using DioramaEnigma.Sequences;
using Extensions.Audio;
using Extensions.Log;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;

namespace DioramaEnigma.Scratch
{
    /// <summary>
    /// Ввод: процент затирания декали
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScratchInteractable : InteractableInput, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Tooltip("Стираемый слой (маска декали)")]
        [SerializeField] private ScratchSurface surface;
        [Tooltip("Слои поверхностей объектов, по которым ловим точку трения")]
        [SerializeField] private LayerMask surfaceMask = ~0;
        [Tooltip("Радиус кисти стирания в долях UV (0..1)")]
        [Range(0.005f, 0.5f)]
        [SerializeField] private float brushRadiusUv = 0.06f;
        [Tooltip("Доля стёртой декали, при которой шаг считается завершённым")]
        [Range(0f, 1f)]
        [SerializeField] private float completionThreshold = 0.85f;

        [Header("Фидбэк (ведётся за курсором)"), Space]
        [Tooltip("Эмиттер пыли (Rate over Distance): двигается за точкой контакта")]
        [SerializeField] private ParticleSystem dustParticles;
        [Tooltip("Звук трения: один зацикленный источник, ведётся за курсором")]
        [SerializeField] private AudioResource scratchSound;

        private Camera mainCamera;
        private AudioSource loopSource;
        private bool completed;
        private bool feedbackActive;

        protected override void Awake()
        {
            base.Awake();

            mainCamera = Camera.main;

            if (surface == null)
                ServiceDebug.LogError(this, $"Не назначен {nameof(surface)} — стирать нечего");
        }

        private void OnEnable()
        {
            if (State == null) return;

            State.onCompletionChanged += OnCompletionChanged;

            completed = State.IsCompleted;
            if (surface != null) surface.SetErased(completed);
        }

        private void OnDisable()
        {
            if (State != null) State.onCompletionChanged -= OnCompletionChanged;

            StopFeedback();
        }

        /// <inheritdoc/>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (State == null || completed) return;

            // Шаг недоступен (нет ресурса/залочен/кулдаун) — отклоняем попытку с фидбэком
            if (!State.CanChangeValue) State.NotifyInteractionRejected();
        }

        /// <inheritdoc/>
        public void OnDrag(PointerEventData eventData)
        {
            if (State == null || completed || surface == null) return;

            if (!State.CanChangeValue)
            {
                StopFeedback();
                return;
            }

            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            Ray ray = mainCamera.ScreenPointToRay(eventData.position);
            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, surfaceMask, QueryTriggerInteraction.Ignore))
            {
                StopFeedback();
                return;
            }

            UpdateFeedback(hit.point, hit.normal);
            surface.TryEraseAt(hit.point, brushRadiusUv);

            if (surface.Progress >= completionThreshold)
                State.SetValue(State.CompletionState);
        }

        /// <inheritdoc/>
        public void OnEndDrag(PointerEventData eventData) => StopFeedback();

        #region Internal

        private void OnCompletionChanged(bool isCompleted)
        {
            completed = isCompleted;
            if (surface != null) surface.SetErased(isCompleted);

            if (isCompleted) StopFeedback();
        }

        /// <summary> Подвести эмиттер и звук к точке контакта (запустить при первом касании) </summary>
        private void UpdateFeedback(Vector3 point, Vector3 normal)
        {
            if (dustParticles != null)
            {
                dustParticles.transform.SetPositionAndRotation(point, Quaternion.LookRotation(normal));

                if (!feedbackActive)
                {
                    dustParticles.Clear();
                    dustParticles.Play();
                }
            }

            if (!feedbackActive)
            {
                if (scratchSound != null && AudioController.Instance != null)
                    loopSource = AudioController.Instance.Play(scratchSound, point, AudioModel.Sfx, loop: true);

                feedbackActive = true;
            }

            if (loopSource != null) loopSource.transform.position = point;
        }

        private void StopFeedback()
        {
            if (!feedbackActive) return;
            feedbackActive = false;

            if (dustParticles != null)
                dustParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            if (loopSource != null)
            {
                if (AudioController.Instance != null) AudioController.Instance.Stop(loopSource);
                loopSource = null;
            }
        }

        #endregion
    }
}
