using Extensions.Generics;
using Extensions.Log;
using UnityEngine;
using UnityEngine.UI;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Кнопка переключения на следующую/предыдущую диораму блока; гаснет (не-interactable) на краю блока
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class DioramaNavigationButtonAction : AbstractButtonAction
    {
        [Tooltip("Канал рантайм-ссылки на спавнер")]
        [SerializeField] private DioramaSpawnerReference reference;
        [Tooltip("Направление переключения")]
        [SerializeField] private DioramaNavigationDirection direction = DioramaNavigationDirection.Next;

        private Button button;
        private DioramaSpawner boundSpawner;

        protected override void Awake()
        {
            base.Awake();
            button = GetComponent<Button>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (reference == null) return;

            reference.onInitialized += OnSpawnerSet;
            reference.onReleased += OnSpawnerReleased;

            if (reference.HasValue) OnSpawnerSet(reference.Current);
            else RefreshInteractable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (reference != null)
            {
                reference.onInitialized -= OnSpawnerSet;
                reference.onReleased -= OnSpawnerReleased;
            }

            Unbind();
        }

        public override void OnButtonClickAction()
        {
            if (boundSpawner == null)
            {
                ServiceDebug.LogError(this, "спавнер недоступен (reference не назначен или сцена не готова)");
                return;
            }

            switch (direction)
            {
                case DioramaNavigationDirection.Next:
                    boundSpawner.FocusNext();
                    break;
                case DioramaNavigationDirection.Previous:
                    boundSpawner.FocusPrev();
                    break;
                default:
                    ServiceDebug.LogError(this, $"Необработанное направление: {direction}");
                    break;
            }
        }

        private void OnSpawnerSet(DioramaSpawner spawner)
        {
            boundSpawner = spawner;
            boundSpawner.onFocusChanged += OnFocusChanged;
            RefreshInteractable();
        }

        private void OnSpawnerReleased()
        {
            Unbind();
            RefreshInteractable();
        }

        private void Unbind()
        {
            if (boundSpawner != null) boundSpawner.onFocusChanged -= OnFocusChanged;
            boundSpawner = null;
        }

        private void OnFocusChanged(DioramaFocus _) => RefreshInteractable();

        private void RefreshInteractable()
        {
            if (button == null) return;

            button.interactable = boundSpawner != null &&
                (direction == DioramaNavigationDirection.Next ? boundSpawner.CanFocusNext : boundSpawner.CanFocusPrev);
        }
    }
}
