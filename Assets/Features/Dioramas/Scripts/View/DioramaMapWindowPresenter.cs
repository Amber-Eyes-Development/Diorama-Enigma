using Extensions.Log;
using Extensions.RuntimeReferences;
using UnityEngine;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Окно-карта активного блока: инстанцирует ручной макет блока и связывает его комнаты со спавнером
    /// </summary>
    /// <remarks>
    /// Динамически инстанцируемое окно: спавнер берётся из канала, сервис доступа — из ассета.
    /// Макет (комнаты-кнопки + двери) задаётся на блоке (`mapLayoutPrefab`); двери самодостаточны (step-view)
    /// </remarks>
    public sealed class DioramaMapWindowPresenter : RuntimeReferenceConsumer<DioramaSpawner>
    {
        [Tooltip("Сервис доступа (ассет)")]
        [SerializeField] private DioramaAccessService access;
        [Tooltip("Контейнер, в который инстанцируется макет карты блока")]
        [SerializeField] private Transform content;

        private DioramaSpawner spawner;
        private GameObject layout;

        protected override void OnInitialized(DioramaSpawner value)
        {
            spawner = value;
            if (access != null) access.onDioramaUnlocked += OnDioramaUnlocked;
            BuildLayout();
        }

        protected override void OnReleased()
        {
            if (access != null) access.onDioramaUnlocked -= OnDioramaUnlocked;
            ClearLayout();
            spawner = null;
        }

        private void OnDioramaUnlocked(DioramaDefinition _) => RefreshRooms();

        private void BuildLayout()
        {
            ClearLayout();

            var block = spawner != null ? spawner.ActiveBlock : null;
            if (block == null) return;

            if (block.MapLayoutPrefab == null)
            {
                ServiceDebug.LogWarning(this, $"У блока {block.name} не задан mapLayoutPrefab");
                return;
            }

            layout = Instantiate(block.MapLayoutPrefab, content);

            foreach (var room in layout.GetComponentsInChildren<DioramaMapRoomButton>(true))
                room.Bind(spawner, access);
        }

        private void RefreshRooms()
        {
            if (layout == null) return;

            foreach (var room in layout.GetComponentsInChildren<DioramaMapRoomButton>(true))
                room.Refresh();
        }

        private void ClearLayout()
        {
            if (layout != null) Destroy(layout);
            layout = null;
        }
    }
}
