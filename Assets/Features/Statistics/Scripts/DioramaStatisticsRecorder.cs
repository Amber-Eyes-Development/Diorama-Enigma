using System.Collections.Generic;
using DioramaEnigma.Dioramas;
using UnityEngine;

namespace DioramaEnigma.Statistics
{
    /// <summary>
    /// Замеряет игровое время, проведённое с каждой диорамой в фокусе — для статистики прохождения блока
    /// </summary>
    /// <remarks> Объект сцены рядом со спавнером; копит время его активной диорамы, снимается окном итогов </remarks>
    public sealed class DioramaStatisticsRecorder : MonoBehaviour
    {
        [Tooltip("Спавнер активного блока (объект сцены)")]
        [SerializeField] private DioramaSpawner spawner;

        private readonly Dictionary<string, float> secondsByDiorama = new();

        private void Update()
        {
            var active = spawner != null ? spawner.Active : null;
            if (active == null) return;

            secondsByDiorama.TryGetValue(active.Id, out float seconds);
            secondsByDiorama[active.Id] = seconds + Time.deltaTime;
        }

        /// <summary> Накопленное время диорамы, секунд </summary>
        /// <param name="def">Диорама</param>
        public float SecondsOf(DioramaDefinition def) =>
            def != null && secondsByDiorama.TryGetValue(def.Id, out float seconds) ? seconds : 0f;
    }
}
