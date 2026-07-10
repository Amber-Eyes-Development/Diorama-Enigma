using System;
using DG.Tweening;
using DioramaEnigma.Dioramas;
using TMPro;
using UnityEngine;

namespace DioramaEnigma.Quests
{
    /// <summary> Слот задания: текст квеста и анимации появления/исчезновения/расфокуса </summary>
    public sealed class QuestSlotView : MonoBehaviour
    {
        [Tooltip("Текст задания")]
        [SerializeField] private TMP_Text label;

        [Header("Анимации")]
        [Tooltip("Появление слота")]
        [SerializeField] private DOTweenAnimation[] appearAnimations;
        [Tooltip("Исчезновение слота перед удалением")]
        [SerializeField] private DOTweenAnimation[] disappearAnimations;
        [Tooltip("Приглушение, когда квест не из сфокусированной диорамы (обратно — при возврате фокуса)")]
        [SerializeField] private DOTweenAnimation[] defocusedAnimations;

        /// <summary> Id шага-источника (ключ слота) </summary>
        public string StepId => stepId;
        /// <summary> Диорама квеста (для фокус-арбитража) </summary>
        public DioramaDefinition Diorama => diorama;

        private string stepId;
        private DioramaDefinition diorama;

        private void Awake()
        {
            SetupGroup(appearAnimations);
            SetupGroup(disappearAnimations);
            SetupGroup(defocusedAnimations);
        }

        /// <summary> Привязать слот к квесту </summary>
        public void Bind(ActiveQuest quest)
        {
            stepId = quest.StepId;
            diorama = quest.Diorama;
            if (label != null) label.text = quest.Text;
        }

        /// <summary> Анимация появления </summary>
        public void PlayAppear() => PlayGroup(appearAnimations);

        /// <summary> Анимация исчезновения перед удалением слота </summary>
        /// <param name="onComplete"> Действие по завершении, напр. удалить слот </param>
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

        /// <summary> Выставить фокус: false — приглушить (чужая диорама), true — вернуть в обычный вид </summary>
        public void SetFocused(bool focused)
        {
            if (defocusedAnimations == null) return;

            foreach (var animation in defocusedAnimations)
            {
                if (animation == null) continue;

                EnsureTween(animation);
                if (focused) animation.DORewind();
                else animation.DORestart();
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

        /// <summary> Завершить твины появления/исчезновения, если они играют (анимируют тот же transform/CanvasGroup) </summary>
        private void CompleteOtherGroups(DOTweenAnimation[] except)
        {
            CompleteGroupIfPlaying(appearAnimations, except);
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
