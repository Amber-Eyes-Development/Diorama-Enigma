using UnityEngine;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Блок — тематический набор диорам (группа/ассет-метка)
    /// </summary>
    [CreateAssetMenu(menuName = "Dioramas/Block", fileName = nameof(DioramaBlock))]
    public sealed class DioramaBlock : DescribedAsset
    {
        /// <summary> Префаб ручного UI-макета карты блока (комнаты-кнопки + двери) — для окна-карты </summary>
        public GameObject MapLayoutPrefab => mapLayoutPrefab;

        [Header("Карта блока")]
        [Tooltip("Префаб вручную собранной карты блока (комнаты-кнопки + двери)")]
        [SerializeField] private GameObject mapLayoutPrefab;
    }
}
