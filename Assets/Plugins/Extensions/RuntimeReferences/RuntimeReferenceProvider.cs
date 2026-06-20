using Extensions.Log;
using UnityEngine;

namespace Extensions.RuntimeReferences
{
    /// <summary>
    /// Публикатор: кладёт компонент этого объекта в канал <see cref="RuntimeReference{T}"/> на время активности
    /// </summary>
    /// <remarks>
    /// Для компонентов, готовых сразу при включении. Компоненты с порядком инициализации публикуют себя
    /// вручную из своего кода, когда готовы. Конкретный наследник закрывает <typeparamref name="T"/> и должен
    /// иметь <c>[RequireComponent(typeof(T))]</c>
    /// </remarks>
    /// <typeparam name="T"> Тип публикуемого компонента (на этом же объекте) </typeparam>
    public abstract class RuntimeReferenceProvider<T> : MonoBehaviour where T : Component
    {
        [Tooltip("Канал, в который публикуется компонент этого объекта")]
        [SerializeField] private RuntimeReference<T> reference;

        private T published;

        protected virtual void OnEnable()
        {
            if (reference == null)
            {
                ServiceDebug.LogError($"{nameof(reference)} не назначен");
                return;
            }

            published = GetComponent<T>();
            if (published == null)
            {
                ServiceDebug.LogError($"{typeof(T).Name} не найден на объекте");
                return;
            }

            reference.Set(published);
        }

        protected virtual void OnDisable()
        {
            if (reference != null && ReferenceEquals(reference.Current, published))
                reference.Clear();

            published = null;
        }
    }
}
