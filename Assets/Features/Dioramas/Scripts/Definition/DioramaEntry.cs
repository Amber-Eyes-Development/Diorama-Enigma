using System;
using System.Collections.Generic;
using UnityEngine;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Запись диорамы в блоке реестра
    /// </summary>
    /// <remarks>
    /// Позиция записи в списке блока задаёт очередность диорамы
    /// </remarks>
    [Serializable]
    public sealed class DioramaEntry
    {
        /// <summary> Определение диорамы </summary>
        public DioramaDefinition Definition => definition;
        /// <summary> Входящие связи: задают и граф связей, и условия открытия </summary>
        public IReadOnlyList<DioramaLink> IncomingLinks => incomingLinks;

        [SerializeField] private DioramaDefinition definition;

        [Header("Доступ")]
        [Tooltip("Входящие связи: каждая задаёт диораму-источник и условие открытия")]
        [SerializeField] private List<DioramaLink> incomingLinks = new();
    }
}
