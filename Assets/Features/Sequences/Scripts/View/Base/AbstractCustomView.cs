using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// База кастомного эффекта вьюшки: контракт для уникального кода, управляемого <see cref="InteractableCustomView"/>
    /// </summary>
    public abstract class AbstractCustomView : MonoBehaviour
    {
        /// <summary> Рантайм: подготовить вьюшку к управлению (напр. снять авто-старт у управляемых компонентов) </summary>
        public virtual void Prepare() { }

        /// <summary> Применить эффект: на срабатывание триггера (reverse=false) или на откат (reverse=true) </summary>
        /// <param name="reverse">Откат: привести к противоположному (исходному) состоянию</param>
        /// <param name="silent">Тихо: без видимого/слышимого проигрыша (восстановление при загрузке)</param>
        public abstract void Apply(bool reverse, bool silent);
    }
}
