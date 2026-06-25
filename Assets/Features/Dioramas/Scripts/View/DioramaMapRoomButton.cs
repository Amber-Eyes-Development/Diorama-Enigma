using Extensions.Generics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Комната диорамы на карте блока: кнопка перехода к диораме; скрыта, пока диорама закрыта
    /// </summary>
    public sealed class DioramaMapRoomButton : AbstractButtonAction
    {
        [Tooltip("Диорама этой комнаты")]
        [SerializeField] private DioramaDefinition diorama;
        [Tooltip("Опционально: подпись комнаты")]
        [SerializeField] private TMP_Text label;

        [Header("Состояние «пройдено»")]
        [Tooltip("Опционально: графика для приглушения пройденной комнаты")]
        [SerializeField] private Graphic tintTarget;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color completedColor = new(0.6f, 0.6f, 0.6f, 1f);

        private DioramaSpawner spawner;
        private DioramaAccessService access;

        /// <summary> Привязать комнату к спавнеру и сервису доступа </summary>
        public void Bind(DioramaSpawner spawner, DioramaAccessService access)
        {
            this.spawner = spawner;
            this.access = access;

            if (label != null && diorama != null) label.text = diorama.Title;
            Refresh();
        }

        /// <summary> Обновить видимость/вид по состоянию доступа </summary>
        public void Refresh()
        {
            if (access == null || diorama == null) return;

            var state = access.StateOf(diorama);
            gameObject.SetActive(state != DioramaState.Locked); // закрытая комната не видна

            if (tintTarget != null)
                tintTarget.color = state == DioramaState.Completed ? completedColor : normalColor;
        }

        public override void OnButtonClickAction()
        {
            if (spawner != null && diorama != null) spawner.FocusDiorama(diorama);
        }
    }
}
