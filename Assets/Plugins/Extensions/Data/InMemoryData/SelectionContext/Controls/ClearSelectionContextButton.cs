using Extensions.Helpers;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Extensions.Data.InMemoryData.SelectionContext
{
    /// <summary>
    /// Кнопка очистки выбора <see cref="BaseSelectionContext"/>
    /// </summary>
    public sealed class ClearSelectionContextButton : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private BaseSelectionContext selectionContext; 
        
        public void OnPointerDown(PointerEventData eventData)
        {
            if (Logic.IsNotNull(selectionContext)) selectionContext.Clear();
        }
    }
}