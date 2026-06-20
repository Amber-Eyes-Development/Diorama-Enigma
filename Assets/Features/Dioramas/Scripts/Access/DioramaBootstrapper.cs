using Extensions.Log;
using UnityEngine;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Точка инициализации системы диорам
    /// </summary>
    public sealed class DioramaBootstrapper : MonoBehaviour
    {
        /// <summary> Сервис доступа (null до инициализации) </summary>
        public DioramaAccessService Access => access;

        [Tooltip("Реестр диорам (единый ассет проекта)")]
        [SerializeField] private DioramaRegistry registry;
        [Tooltip("Локатор для динамического UI — сюда публикуется сервис доступа")]
        [SerializeField] private DioramaContext context;
        [Tooltip("Инициализировать сервис в Start (после выбора профиля сохранения)")]
        [SerializeField] private bool initializeOnStart = true;

        private DioramaAccessService access;

        private void Start()
        {
            if (initializeOnStart) Initialize();
        }

        private void OnDestroy()
        {
            access?.Dispose();
            if (context != null) context.SetAccess(null);
        }

        /// <summary> Создать и инициализировать сервис доступа (идемпотентно) </summary>
        public DioramaAccessService Initialize()
        {
            if (access != null) return access;

            if (registry == null)
            {
                ServiceDebug.LogError(this, "registry не назначен");
                return null;
            }

            access = new DioramaAccessService(registry);
            access.Initialize();

            if (context != null) context.SetAccess(access);

            return access;
        }
    }
}
