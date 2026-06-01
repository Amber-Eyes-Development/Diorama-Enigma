using Extensions.Generics;
using Extensions.Log;
using UnityEngine;

namespace Extensions.SceneFlow
{
    /// <summary>
    /// Открыть сцену по зажатию кнопки
    /// </summary>
    public class LoadSceneHoldButtonAction : AbstractHoldButtonAction
    {
        [SerializeField]
        protected SceneID targetScene = default;

        [SerializeField]
        protected bool additive = false;

        public override void OnButtonClickAction()
        {
            if (SceneController.Instance == null)
            {
                ServiceDebug.LogError($"{nameof(SceneController)} не найден");
                return;
            }

            if (targetScene == null)
            {
                ServiceDebug.LogError($"Не назначена целевая сцена");
                return;
            }

            SceneController.Instance.LoadSceneByID(targetScene.Id, additive);
        }
    }
}