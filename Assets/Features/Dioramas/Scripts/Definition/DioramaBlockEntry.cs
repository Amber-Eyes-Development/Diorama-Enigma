using System;
using System.Collections.Generic;
using UnityEngine;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Запись реестра: блок и его диорамы в порядке прохождения
    /// </summary>
    /// <remarks>
    /// Порядок записей диорам в списке задаёт их очередность
    /// </remarks>
    [Serializable]
    public sealed class DioramaBlockEntry
    {
        /// <summary> Блок </summary>
        public DioramaBlock Block => block;
        /// <summary> Записи диорам блока в порядке прохождения </summary>
        public IReadOnlyList<DioramaEntry> Dioramas => dioramas;

        [SerializeField] private DioramaBlock block;
        [SerializeField] private List<DioramaEntry> dioramas = new();
    }
}
