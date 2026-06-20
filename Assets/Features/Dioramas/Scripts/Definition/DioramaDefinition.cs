using DioramaEnigma.Sequences;
using UnityEngine;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Определение диорамы
    /// </summary>
    [CreateAssetMenu(menuName = "Dioramas/Diorama", fileName = nameof(DioramaDefinition))]
    public sealed class DioramaDefinition : DescribedAsset
    {
        /// <summary> Префаб диорамы (раннер в корне) — инстанцируется на сцену </summary>
        public SequenceRunner RunnerPrefab => runnerPrefab;
        /// <summary> Последовательность диорамы (та же, что у раннера в префабе) </summary>
        public Sequence Sequence => sequence;

        /// <summary>
        /// Пройдена ли диорама целиком прямо сейчас (выводится из последовательности; учитывает откат шагов)
        /// </summary>
        public bool IsCompleted => sequence != null && sequence.IsCompleted;

        [Header("Диорама")]
        [Tooltip("Префаб диорамы — в корне должен быть SequenceRunner")]
        [SerializeField] private SequenceRunner runnerPrefab;
        [Tooltip("Последовательность диорамы (та же, что назначена раннеру в префабе)")]
        [SerializeField] private Sequence sequence;
    }
}
