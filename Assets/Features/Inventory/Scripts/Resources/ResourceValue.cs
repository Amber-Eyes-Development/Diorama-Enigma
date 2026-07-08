using Extensions.ScriptableValues;
using UnityEngine;

namespace DioramaEnigma.Inventory
{
    /// <summary> Предмет инвентаря: целочисленный ресурс с иконкой и названием для UI </summary>
    [CreateAssetMenu(menuName = "Inventory/Item", fileName = nameof(ResourceValue))]
    public sealed class ResourceValue : IntValue
    {
        /// <summary> Название предмета для UI </summary> // TODO строку в локализацию
        public string Title => title;
        /// <summary> Иконка предмета для UI (слот инвентаря) </summary>
        public Sprite Icon => icon;

        [Header("Предмет"), Space]
        [Tooltip("Строка, выводимая в подсказке при наводке на ресурс в UI")]
        [SerializeField] private string title;
        [SerializeField] private Sprite icon;
    }
}
