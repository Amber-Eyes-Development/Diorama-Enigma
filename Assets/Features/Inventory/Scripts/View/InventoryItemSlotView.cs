using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DioramaEnigma.Inventory
{
    /// <summary> Слот инвентаря: иконка, количество и анимации появления/использования/исчезновения </summary>
    public sealed class InventoryItemSlotView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [Tooltip("Подпись количества (скрыта при количестве <= 1)")]
        [SerializeField] private TMP_Text quantityLabel;

        [Header("Анимации (несколько твинов запускаются вместе)")]
        [Tooltip("Анимация появления слота")]
        [SerializeField] private DOTweenAnimation[] appearAnimations;
        [Tooltip("Анимация исчезновения слота перед удалением")]
        [SerializeField] private DOTweenAnimation[] disappearAnimations;
        [Tooltip("Анимация использования (трата ресурса, слот остаётся)")]
        [SerializeField] private DOTweenAnimation[] useAnimations;
        [Tooltip("Анимация «этот предмет сейчас нужен» при наведении на нужный объект")]
        [SerializeField] private DOTweenAnimation[] neededAnimations;

        /// <summary> Привязанный предмет </summary>
        public ResourceValue Item => item;
        /// <summary> Показываемое сейчас количество </summary>
        public int Quantity => quantity;

        private ResourceValue item;
        private int quantity;

        private void Awake()
        {
            SetupGroup(appearAnimations);
            SetupGroup(disappearAnimations);
            SetupGroup(useAnimations);
            SetupGroup(neededAnimations);
        }

        /// <summary> Привязать слот к предмету и показать его количество </summary>
        public void Bind(ResourceValue item)
        {
            this.item = item;
            if (icon != null) icon.sprite = item != null ? item.Icon : null;
            SetQuantity(item != null ? item.Value : 0);
        }

        /// <summary> Обновить отображаемое количество </summary>
        public void SetQuantity(int value)
        {
            quantity = value;
            if (quantityLabel == null) return;

            quantityLabel.text = value.ToString();
            quantityLabel.gameObject.SetActive(value > 1);
        }

        /// <summary> Анимация появления предмета </summary>
        public void PlayAppear() => PlayGroup(appearAnimations);

        /// <summary> Анимация использования (трата ресурса, слот остаётся) </summary>
        public void PlayUse() => PlayGroup(useAnimations);

        /// <summary> Анимация исчезновения перед удалением слота </summary>
        /// <param name="onComplete">Действие по завершении, напр. удалить слот</param>
        public void PlayDisappear(Action onComplete = null)
        {
            DOTweenAnimation primary = FindFirst(disappearAnimations);
            if (primary == null)
            {
                onComplete?.Invoke();
                return;
            }

            PlayGroup(disappearAnimations);
            primary.tween.OnComplete(() => onComplete?.Invoke());
        }

        /// <summary> Показать/снять, что предмет нужен для наведённого объекта </summary>
        public void SetNeeded(bool needed)
        {
            if (neededAnimations == null) return;

            foreach (var animation in neededAnimations)
            {
                if (animation == null) continue;

                EnsureTween(animation);
                if (needed) animation.DORestart();
                else animation.DORewind();
            }
        }

        /// <summary> Запустить все твины группы, прервав твины остальных групп </summary>
        private void PlayGroup(DOTweenAnimation[] group)
        {
            if (group == null || group.Length == 0) return;

            CompleteOtherGroups(group);
            foreach (var animation in group)
            {
                if (animation == null) continue;

                EnsureTween(animation);
                animation.DORestart();
            }
        }

        /// <summary> Завершить твины остальных групп, если они сейчас играют (все анимируют один и тот же transform/CanvasGroup) </summary>
        private void CompleteOtherGroups(DOTweenAnimation[] except)
        {
            CompleteGroupIfPlaying(appearAnimations, except);
            CompleteGroupIfPlaying(useAnimations, except);
            CompleteGroupIfPlaying(disappearAnimations, except);
        }

        private static void CompleteGroupIfPlaying(DOTweenAnimation[] group, DOTweenAnimation[] except)
        {
            if (group == null || group == except) return;

            foreach (var animation in group)
                if (animation != null && animation.tween != null && animation.tween.IsPlaying())
                    animation.DOComplete();
        }

        private static void SetupGroup(DOTweenAnimation[] group)
        {
            if (group == null) return;

            foreach (var animation in group)
            {
                if (animation == null) continue;

                animation.autoGenerate = false;
                animation.autoPlay = false;
                animation.autoKill = false;
            }
        }

        /// <summary> Создать твин, если его ещё нет (вью сама управляет проигрыванием) </summary>
        private static void EnsureTween(DOTweenAnimation animation)
        {
            if (animation.tween == null)
                animation.CreateTween(regenerateIfExists: false, andPlay: false);
        }

        private static DOTweenAnimation FindFirst(DOTweenAnimation[] group)
        {
            if (group == null) return null;

            foreach (var animation in group)
                if (animation != null) return animation;

            return null;
        }
    }
}
