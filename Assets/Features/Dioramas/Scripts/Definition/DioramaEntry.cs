using System;
using System.Collections.Generic;
using Extensions.Attributes;
using Extensions.Helpers.Enumerations;
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
        /// <summary> Как объединять входящие связи: <see cref="LogicOperator.And"/> — все, <see cref="LogicOperator.Or"/> — любая </summary>
        public LogicOperator LinkOperator => linkOperator;
        /// <summary> Входящие связи: задают и граф связей, и условия открытия </summary>
        public IReadOnlyList<DioramaLink> IncomingLinks => incomingLinks;

        [SerializeField] private DioramaDefinition definition;

        [Header("Доступ")]
        [Tooltip("Как объединять входящие связи: И — выполнены все, ИЛИ — хотя бы одна. " +
                 "Если связей нет — диорама открывается по решению предыдущей записи реестра")]
        [EnumRange(0, 1)]
        [SerializeField] private LogicOperator linkOperator = LogicOperator.Or;
        [Tooltip("Входящие связи: каждая задаёт диораму-источник и условие открытия")]
        [SerializeField] private List<DioramaLink> incomingLinks = new();
    }
}
