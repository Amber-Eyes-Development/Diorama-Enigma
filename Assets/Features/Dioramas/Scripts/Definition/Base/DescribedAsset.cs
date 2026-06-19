using Extensions.Identification;
using UnityEngine;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Базовый идентифицируемый ассет домена диорам с названием и описанием
    /// </summary>
    public abstract class DescribedAsset : IdentifiableObject
    {
        /// <summary> Название </summary> // TODO строку в локализацию
        public string Title => title;
        /// <summary> Описание </summary> // TODO строку в локализацию
        public string Description => description;

        [Header("Описание"), Space]
        [SerializeField] private string title;
        [TextArea]
        [SerializeField] private string description;
    }
}
