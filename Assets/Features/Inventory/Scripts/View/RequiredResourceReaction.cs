using System.Collections.Generic;
using DioramaEnigma.Sequences;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DioramaEnigma.Inventory
{
    /// <summary> Наведение на объект помечает в инвентаре предметы, требуемые его шагом </summary>
    [RequireComponent(typeof(StepReference))]
    public sealed class RequiredResourceReaction : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("Канал панели инвентаря")]
        [SerializeField] private InventoryViewReference viewReference;

        private AbstractSequenceStep step;
        private readonly List<ResourceValue> needed = new();

        private void Awake() => step = GetComponent<StepReference>().Step;

        public void OnPointerEnter(PointerEventData eventData)
        {
            var view = viewReference != null ? viewReference.Current : null;
            if (view == null || step?.ActiveGates == null) return;

            foreach (var gate in step.ActiveGates)
            {
                if (gate is not RequireResourceGate requirement || requirement.Resource == null) continue;

                view.SetItemNeeded(requirement.Resource, true);
                needed.Add(requirement.Resource);
            }
        }

        public void OnPointerExit(PointerEventData eventData) => Clear();

        private void Clear()
        {
            var view = viewReference != null ? viewReference.Current : null;
            if (view != null)
                foreach (var resource in needed)
                    view.SetItemNeeded(resource, false);

            needed.Clear();
        }

        private void OnDisable() => Clear();
    }
}
