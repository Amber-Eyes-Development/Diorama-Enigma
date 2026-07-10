using DG.Tweening;
using Extensions.Log;
using UnityEngine;

namespace Extensions.ScriptableValues
{
    /// <summary>
    /// Проигрывает DOTweenAnimation по изменению значения ScriptableValue и по длительному простою (неизменению)
    /// </summary>
    public sealed class ScriptableValueAnimator : MonoBehaviour
    {
        [Tooltip("Наблюдаемое значение (любой конкретный ScriptableValue)")]
        [SerializeField] private BaseScriptableValue scriptableValue;
        
        [Header("Анимации"), Space]
        [Tooltip("Анимация при изменении значения")]
        [SerializeField] private DOTweenAnimation onChangeAnimation;
        [Tooltip("Анимация при простое — значение не менялось указанное время")]
        [SerializeField] private DOTweenAnimation onIdleAnimation;
        
        [Header("Параметры"), Space]
        [Tooltip("Время простоя до запуска анимации простоя, сек")]
        [Min(0f)]
        [SerializeField] private float idleDelay = 2f;
        [Tooltip("Не запускать анимацию простоя и не считать время простоя, пока значение не равно дефолтному")]
        [SerializeField] private bool skipIdleWhenNotDefault;
        [Tooltip("Не переигрывать анимацию изменения, пока идёт отсчёт простоя — только продлевать (сбрасывать) таймер")]
        [SerializeField] private bool skipChangeWhileIdle;

        private float idleTimer;
        private bool idlePlayed;
        private bool shown;

        private void OnEnable()
        {
            if (scriptableValue == null)
            {
                ServiceDebug.LogError($"{nameof(scriptableValue)} не назначен");
                enabled = false;
                return;
            }

            scriptableValue.onChanged += OnValueChanged;
            ResetIdle();
        }

        private void OnDisable()
        {
            if (scriptableValue != null) scriptableValue.onChanged -= OnValueChanged;
        }

        private void Update()
        {
            if (idlePlayed) return;

            // Пока значение не дефолтное (при включённой опции) — простой не копится
            if (skipIdleWhenNotDefault && !scriptableValue.IsDefault)
            {
                idleTimer = 0f;
                return;
            }

            idleTimer += Time.unscaledDeltaTime;
            if (idleTimer >= idleDelay) PlayIdle();
        }

        private void OnValueChanged()
        {
            bool wasVisible = shown;
            ResetIdle();

            // Пока цель показана (идёт отсчёт простоя) — не переигрываем анимацию, только продлеваем видимость
            if (skipChangeWhileIdle && wasVisible) return;

            PlayChange();
        }

        private void ResetIdle()
        {
            idleTimer = 0f;
            idlePlayed = false;
        }

        // Останавливаем встречную анимацию, чтобы не конфликтовали на одном свойстве (напр. alpha CanvasGroup)
        private void PlayChange()
        {
            shown = true;

            if (onIdleAnimation != null) onIdleAnimation.DOPause();
            if (onChangeAnimation != null) onChangeAnimation.DORestart();
        }

        private void PlayIdle()
        {
            idlePlayed = true;
            shown = false;

            if (onChangeAnimation != null) onChangeAnimation.DOPause();
            if (onIdleAnimation != null) onIdleAnimation.DORestart();
        }
    }
}
