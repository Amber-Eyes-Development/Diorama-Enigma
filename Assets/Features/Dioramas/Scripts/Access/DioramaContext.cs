using System;
using UnityEngine;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Рантайм-локатор системы диорам со сцены (для передачи в UI)
    /// </summary>
    [CreateAssetMenu(menuName = "Dioramas/Context", fileName = nameof(DioramaContext))]
    public sealed class DioramaContext : ScriptableObject
    {
        /// <summary> Состав сервисов изменился (опубликован/снят сервис) </summary>
        public event Action onChanged;

        /// <summary> Сервис доступа (null пока сцена не готова) </summary>
        public DioramaAccessService Access { get; private set; }
        /// <summary> Спавнер диорам (null пока сцена не готова) </summary>
        public DioramaSpawner Spawner { get; private set; }

        /// <summary> Опубликовать/снять сервис доступа </summary>
        public void SetAccess(DioramaAccessService access)
        {
            Access = access;
            onChanged?.Invoke();
        }

        /// <summary> Опубликовать/снять спавнер </summary>
        public void SetSpawner(DioramaSpawner spawner)
        {
            Spawner = spawner;
            onChanged?.Invoke();
        }
    }
}
