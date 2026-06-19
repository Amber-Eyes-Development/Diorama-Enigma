using System.Collections.Generic;
using DioramaEnigma.Sequences;
using UnityEngine;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Определение диорамы
    /// </summary>
    /// <remarks>
    /// Принадлежность к блоку и порядок задаются в <see cref="DioramaRegistry"/>
    /// </remarks>
    [CreateAssetMenu(menuName = "Dioramas/Diorama", fileName = nameof(DioramaDefinition))]
    public sealed class DioramaDefinition : DescribedAsset
    {
        /// <summary> Префаб диорамы (раннер в корне) — инстанцируется на сцену </summary>
        public SequenceRunner RunnerPrefab => runnerPrefab;
        /// <summary> Последовательность диорамы (та же, что у раннера в префабе) </summary>
        public Sequence Sequence => sequence;
        /// <summary> Открыта ли с самого начала игры (стартовая диорама) </summary>
        public bool UnlockedFromStart => unlockedFromStart;
        /// <summary> Входящие связи: задают и граф связей, и условия открытия </summary>
        public IReadOnlyList<DioramaLink> IncomingLinks => incomingLinks;

        /// <summary>
        /// Пройдена ли диорама целиком прямо сейчас (выводится из последовательности; учитывает откат шагов)
        /// </summary>
        public bool IsCompleted => sequence != null && sequence.IsCompleted;

        [Header("Диорама")]
        [Tooltip("Префаб диорамы — в корне должен быть SequenceRunner")]
        [SerializeField] private SequenceRunner runnerPrefab;
        [Tooltip("Последовательность диорамы (та же, что назначена раннеру в префабе)")]
        [SerializeField] private Sequence sequence;

        [Header("Доступ")]
        [Tooltip("Открыта ли диорама сразу при старте новой игры")]
        [SerializeField] private bool unlockedFromStart;
        [Tooltip("Входящие связи: каждая задаёт диораму-источник и условие открытия")]
        [SerializeField] private List<DioramaLink> incomingLinks = new();
    }
}
