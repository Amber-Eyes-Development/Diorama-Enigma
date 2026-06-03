using Extensions.Audio;
using Extensions.Log;
using UnityEngine;

namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Абстракция звуковой реакции на <see cref="InteractableObject"/>
    /// </summary>
    [RequireComponent(typeof(InteractableObject))]
    public abstract class InteractableAudioBehaviour : BaseAudioPlayer
    {
        /// <summary> Связанный объект </summary>
        protected InteractableObject Interactable { get; private set; }

        protected virtual void Awake()
        {
            Interactable = GetComponent<InteractableObject>();

            if (Interactable == null)
                ServiceDebug.LogWarning(this, "InteractableObject не найден на этом объекте");
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (Interactable != null) Subscribe();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (Interactable != null) Unsubscribe();
        }

        /// <summary> Подписаться на события объекта </summary>
        protected abstract void Subscribe();

        /// <summary> Отписаться от событий объекта </summary>
        protected abstract void Unsubscribe();
    }
}
