using UnityEngine;
using UnityEngine.UI;

namespace Extensions.SpriteTools
{
    /// <summary>
    /// Покадровая анимаций для UI Image, последовательно перебирая спрайты
    /// </summary>
    public class UIImageAnimator : MonoBehaviour
    {
        #region Параметры
        
        [Header("Изображение"), Space]
        [SerializeField] protected Sprite[] frames;
        [SerializeField] protected float framesPerSecond = 12f;
        
        [Header("Параметры анимации"), Space]
        [SerializeField] protected bool loop = true;
        [SerializeField] protected bool playOnEnable = true;
        
        #endregion
        
        protected Image targetImage;

        protected int currentFrameIndex;
        protected float timer;
        protected bool isPlaying;

        protected void Awake() => targetImage = GetComponent<Image>();

        protected void OnEnable()
        {
            if (playOnEnable) Play();
        }

        protected void Update()
        {
            if (!isPlaying 
                || frames == null || frames.Length == 0 
                || framesPerSecond <= 0f)
            {
                return;
            }

            timer += Time.deltaTime;
            float frameDuration = 1f / framesPerSecond;

            while (timer >= frameDuration)
            {
                timer -= frameDuration;
                AdvanceFrame();
            }
        }

        #region Управление воспроизведением

        /// <summary>
        /// Запуск воспроизведения анимации с начала
        /// </summary>
        public void Play()
        {
            currentFrameIndex = 0;
            timer = 0f;
            isPlaying = true;

            ApplyFrame();
        }

        /// <summary>
        /// Остановка воспроизведения анимации
        /// </summary>
        public void Stop()
        {
            isPlaying = false;
        }

        /// <summary>
        /// Пауза воспроизведения анимации
        /// </summary>
        public void Pause() => isPlaying = false;

        /// <summary>
        /// Продолжение воспроизведения анимации
        /// </summary>
        public void Resume() => isPlaying = true;
        
        #endregion

        protected void AdvanceFrame()
        {
            currentFrameIndex++;

            if (currentFrameIndex >= frames.Length)
            {
                if (loop)
                    currentFrameIndex = 0;
                else
                {
                    currentFrameIndex = frames.Length - 1;
                    Stop();
                }
            }

            ApplyFrame();
        }

        protected void ApplyFrame()
        {
            if (targetImage == null) return;

            targetImage.sprite = frames[currentFrameIndex];
        }
    }
}