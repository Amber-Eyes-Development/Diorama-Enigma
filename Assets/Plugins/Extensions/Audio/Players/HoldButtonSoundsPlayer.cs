using Extensions.Generics;
using Extensions.Log;
using UnityEngine;
using UnityEngine.Audio;

namespace Extensions.Audio
{
    /// <summary>
    /// Воспроизведение звуков кнопкой с удержанием
    /// </summary>
    [RequireComponent(typeof(HoldButtonClickOrchestrator))]
    public class HoldButtonSoundsPlayer : BaseAudioPlayer
    {
        [Header("Звуки"), Space]
        [SerializeField] protected AudioResource onHoldCompleteSound;
        // TODO добавить звук процесса зажатия
        
        protected HoldButtonClickOrchestrator orchestrator;

        protected virtual void Awake()
        {
            orchestrator = GetComponent<HoldButtonClickOrchestrator>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (orchestrator == null)
            {
                ServiceDebug.LogError("Кнопка не назначена");
                return;
            }
            orchestrator.onHoldCompleted += PlayOnHoldCompleted;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (orchestrator == null) return;
            orchestrator.onHoldCompleted -= PlayOnHoldCompleted;
        }
        
        protected void PlayOnHoldCompleted()
        {
            if (onHoldCompleteSound == null) return;
            Play(onHoldCompleteSound);
        }
    }
}