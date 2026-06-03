using Extensions.Log;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Абстракция визуальной реакции на <see cref="InteractableObject"/>
    /// </summary>
    [RequireComponent(typeof(InteractableObject))]
    public abstract class InteractableViewBehaviour : MonoBehaviour
    {
        /// <summary> Связанный объект </summary>
        protected InteractableObject Interactable { get; private set; }

        protected virtual void Awake()
        {
            Interactable = GetComponent<InteractableObject>();

            if (Interactable == null)
                ServiceDebug.LogWarning(this, "InteractableObject не найден на этом объекте");
        }

        protected virtual void OnEnable()
        {
            if (Interactable != null) Subscribe();
        }

        protected virtual void OnDisable()
        {
            if (Interactable != null) Unsubscribe();
        }

        /// <summary> Подписаться на события объекта </summary>
        protected abstract void Subscribe();

        /// <summary> Отписаться от событий объекта </summary>
        protected abstract void Unsubscribe();
    }
}
