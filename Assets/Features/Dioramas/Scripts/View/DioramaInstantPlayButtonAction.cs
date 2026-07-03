using Extensions.Log;
using Extensions.SceneFlow;
using UnityEngine;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Кнопка мгновенного старта: выбирает последнюю открытую по прогрессу диораму и сразу грузит игровую сцену
    /// </summary>
    /// <remarks> Альтернатива связке «выбор диорамы кнопкой + <see cref="LoadSceneButtonAction"/>» </remarks>
    public sealed class DioramaInstantPlayButtonAction : LoadSceneButtonAction
    {
        [Header("Мгновенный старт"), Space]
        [Tooltip("Сервис доступа (ассет)")]
        [SerializeField] private DioramaAccessService access;
        [Tooltip("Выбор блока (ассет-мост)")]
        [SerializeField] private DioramaBlockSelection blockSelection;
        [Tooltip("Выбор диорамы (ассет-мост)")]
        [SerializeField] private DioramaSelection dioramaSelection;

        public override void OnButtonClickAction()
        {
            if (!SelectLastUnlocked()) return;
            base.OnButtonClickAction();
        }

        private bool SelectLastUnlocked()
        {
            if (access == null)
            {
                ServiceDebug.LogError($"{nameof(access)} не назначен");
                return false;
            }

            var def = access.LastUnlockedDiorama();
            if (def == null)
            {
                ServiceDebug.LogWarning("Нет открытых диорам для старта");
                return false;
            }

            var block = access.BlockOf(def);
            if (block != null && blockSelection != null) blockSelection.Select(block.Id);
            if (dioramaSelection != null) dioramaSelection.Select(def.Id);
            return true;
        }
    }
}
