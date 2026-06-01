using Extensions.Log;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Extensions.Data.InMemoryData.SelectionContext
{
    /// <summary>
    /// Кнопка назначения контекста данных
    /// </summary>
    [RequireComponent(typeof(ContextIdHolder))]
    public class AssignSelectionContextButton : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] protected BaseSelectionContext selectionContext;

        protected ContextIdHolder idHolder;

        protected virtual void Awake() => idHolder = GetComponent<ContextIdHolder>();

        public virtual void OnPointerDown(PointerEventData eventData)
        {
            if (selectionContext == null || !idHolder.IsInitialized)
            {
                ServiceDebug.LogError("Инициализация не выполнена или не полностью выполнена");
                return;
            }

            selectionContext.Select(idHolder.EntryId);
        }
    }
}