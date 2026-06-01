using UnityEngine;

namespace DioramaEnigma.Settings
{
    /// <summary>
    /// Контроллер настроек игры
    /// </summary>
    public sealed class SettingsController : MonoBehaviour
    {
        [SerializeField]
        [Range(0, 240)]
        private int targetFrameRate = 60;
        
        private void Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFrameRate;
        }
    }
}