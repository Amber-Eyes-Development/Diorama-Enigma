using System;
using System.Collections.Generic;
using UnityEngine;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Запись реестра: блок и его диорамы в порядке прохождения
    /// </summary>
    /// <remarks>
    /// Порядок диорам в списке задаёт их очередность и принадлежность к блоку
    /// </remarks>
    [Serializable]
    public sealed class DioramaBlockEntry
    {
        /// <summary> Блок </summary>
        public DioramaBlock Block => block;
        /// <summary> Диорамы блока в порядке прохождения </summary>
        public IReadOnlyList<DioramaDefinition> Dioramas => dioramas;

        [SerializeField] private DioramaBlock block;
        [SerializeField] private List<DioramaDefinition> dioramas = new();
    }
}
