using Extensions.Generics;
using Extensions.Log;
using UnityEngine;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Кнопка переключения на следующую/предыдущую открытую диораму
    /// </summary>
    public sealed class DioramaNavigationButtonAction : AbstractButtonAction
    {
        [Tooltip("Канал рантайм-ссылки на спавнер")]
        [SerializeField] private DioramaSpawnerReference reference;
        [Tooltip("Направление переключения")]
        [SerializeField] private DioramaNavigationDirection direction = DioramaNavigationDirection.Next;

        public override void OnButtonClickAction()
        {
            var spawner = reference != null ? reference.Current : null;
            if (spawner == null)
            {
                ServiceDebug.LogError(this, "спавнер недоступен (reference не назначен или сцена не готова)");
                return;
            }

            switch (direction)
            {
                case DioramaNavigationDirection.Next:
                    spawner.FocusNext();
                    break;
                case DioramaNavigationDirection.Previous:
                    spawner.FocusPrev();
                    break;
                default:
                    ServiceDebug.LogError(this, $"Необработанное направление: {direction}");
                    break;
            }
        }
    }
}
