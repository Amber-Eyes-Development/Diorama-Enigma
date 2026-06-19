using Extensions.Log;
using UnityEngine;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Точка инициализации системы диорам
    /// </summary>
    /// <remarks>
    /// Владеет <see cref="DioramaAccessService"/>, связывает его с реестром
    /// <see cref="DioramaRegistry"/> и снимком прогресса.
    /// Инициализировать в Start (после выбора профиля), либо вызвать <see cref="Initialize"/> вручную
    /// </remarks>
    public sealed class DioramaBootstrapper : MonoBehaviour
    {
        /// <summary> Сервис доступа (null до инициализации) </summary>
        public DioramaAccessService Access => access;

        [Tooltip("Реестр диорам (единый ассет проекта)")]
        [SerializeField] private DioramaRegistry registry;
        [Tooltip("Снимок пройденных диорам — страховка прогресса")]
        [SerializeField] private DioramaCompletedSnapshot completedSnapshot;
        [Tooltip("Инициализировать сервис в Start (после выбора профиля сохранения)")]
        [SerializeField] private bool initializeOnStart = true;

        private DioramaAccessService access;

        private void Start()
        {
            if (initializeOnStart) Initialize();
        }

        private void OnDestroy() => access?.Dispose();

        /// <summary> Создать и инициализировать сервис доступа (идемпотентно) </summary>
        public DioramaAccessService Initialize()
        {
            if (access != null) return access;

            if (registry == null)
            {
                ServiceDebug.LogError(this, "registry не назначен");
                return null;
            }

            access = new DioramaAccessService(registry, completedSnapshot);
            access.Initialize();

            return access;
        }
    }
}
