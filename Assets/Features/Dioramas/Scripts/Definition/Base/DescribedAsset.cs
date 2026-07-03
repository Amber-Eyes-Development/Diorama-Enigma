using Extensions.Identification;
using UnityEngine;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Базовый идентифицируемый ассет домена диорам
    /// </summary>
    public abstract class DescribedAsset : IdentifiableObject
    {
        /// <summary> Название </summary> // TODO строку в локализацию
        public string Title => title;
        /// <summary> Описание </summary> // TODO строку в локализацию
        public string Description => description;
        /// <summary> Иконка для UI (кнопки списка/карты) </summary>
        public Sprite Icon => icon;

        [Header("Описание"), Space]
        [SerializeField] private string title;
        [TextArea]
        [SerializeField] private string description;
        [SerializeField] private Sprite icon;
    }
}
