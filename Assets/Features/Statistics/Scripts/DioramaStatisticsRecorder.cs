using DioramaEnigma.Dioramas;
using UnityEngine;

namespace DioramaEnigma.Statistics
{
    /// <summary>
    /// Замеряет игровое время, проведённое с каждой диорамой в фокусе — для статистики прохождения блока
    /// </summary>
    public sealed class DioramaStatisticsRecorder : MonoBehaviour
    {
        [Tooltip("Спавнер активного блока (объект сцены)")]
        [SerializeField] private DioramaSpawner spawner;
        [Tooltip("Как часто сбрасывать накопленное время на диск, сек")]
        [Min(0.5f)]
        [SerializeField] private float flushInterval = 5f;

        private DioramaDefinition current;
        private float pending;   
        private float sinceFlush;

        private void Update()
        {
            var active = spawner != null ? spawner.Active : null;
            if (!ReferenceEquals(active, current))
            {
                Flush();
                current = active;
            }

            if (current == null) return;

            pending += Time.deltaTime;
            sinceFlush += Time.deltaTime;
            if (sinceFlush >= flushInterval) Flush();
        }

        private void OnDisable() => Flush(); 

        private void Flush()
        {
            if (current != null && pending > 0f)
                DioramaStatisticsStore.AddDioramaSeconds(current.Id, pending);

            pending = 0f;
            sinceFlush = 0f;
        }

        /// <summary> Полное накопленное время диорамы (персист + ещё не сохранённое текущее), сек </summary>
        /// <param name="def">Диорама</param>
        public float SecondsOf(DioramaDefinition def)
        {
            if (def == null) return 0f;

            float stored = DioramaStatisticsStore.LoadDioramaSeconds(def.Id);
            return stored + (ReferenceEquals(def, current) ? pending : 0f);
        }
    }
}
