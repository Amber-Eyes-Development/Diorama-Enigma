using System;
using UnityEngine;
using UnityEngine.UI;

namespace Extensions.Generics
{
    /// <summary>
    /// Абстракция изображения
    /// </summary>
    [RequireComponent(typeof(Image))]
    public abstract class AbstractImage : MonoBehaviour
    {
        /// <summary>
        /// Событие, вызываемое после изменения спрайта
        /// </summary>
        public event Action<Sprite> onSpriteUpdated;
        /// <summary>
        /// Событие, вызываемое после изменения цвета
        /// </summary>
        public event Action<Color> onColorUpdated;

        /// <summary>
        /// Спрайт компонента <see cref="Image"/>
        /// </summary>
        public Sprite Sprite => image.sprite;
        /// <summary>
        /// Цвет спрайта компонента <see cref="Image"/>
        /// </summary>
        public Color Color => image.color;
        
        protected Image image;

        protected virtual void Awake() => image = GetComponent<Image>();

        /// <summary>
        /// Установить спрайт изображения
        /// </summary>
        public virtual void SetSprite(Sprite sprite)
        {
            if (image.sprite == sprite) return;

            image.sprite = sprite;
            OnSpriteUpdated(sprite);
            onSpriteUpdated?.Invoke(sprite);
        }

        /// <summary>
        /// Установить цвет изображения
        /// </summary>
        public virtual void SetColor(Color color)
        {
            if (image.color == color) return;

            image.color = color;
            OnColorUpdated(color);
            onColorUpdated?.Invoke(color);
        }

        /// <summary>
        /// Код, выполняемый при изменении спрайта
        /// </summary>
        public virtual void OnSpriteUpdated(Sprite sprite) { }

        /// <summary>
        /// Код, выполняемый при изменении цвета
        /// </summary>
        public virtual void OnColorUpdated(Color color) { }
    }
}