using System;
using System.Collections.Generic;
using UnityEngine;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Реестр диорам: упорядоченный список блоков, каждый со своим упорядоченным списком диорам.
    /// Единый источник истины для очерёдности и принадлежности к блокам в рантайме
    /// </summary>
    [CreateAssetMenu(menuName = "Dioramas/Registry", fileName = nameof(DioramaRegistry))]
    public sealed class DioramaRegistry : ScriptableObject
    {
        /// <summary> Записи блоков в порядке прохождения </summary>
        public IReadOnlyList<DioramaBlockEntry> Blocks => blocks;

        [SerializeField] private List<DioramaBlockEntry> blocks = new();

        private Dictionary<string, DioramaDefinition> byId;
        private Dictionary<DioramaDefinition, DioramaBlock> blockByDiorama;
        private List<DioramaDefinition> ordered;

        /// <summary>
        /// Определение по идентификатору (или null)
        /// </summary>
        /// <param name="id">Идентификатор диорамы</param>
        public DioramaDefinition ById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            EnsureIndex();
            return byId.TryGetValue(id, out var def) ? def : null;
        }

        /// <summary>
        /// Все диорамы в порядке: блок за блоком, внутри блока — по порядку списка
        /// </summary>
        public IReadOnlyList<DioramaDefinition> Ordered()
        {
            EnsureIndex();
            return ordered;
        }

        /// <summary>
        /// Блок, которому принадлежит диорама (или null, если не числится в реестре)
        /// </summary>
        /// <param name="def">Определение диорамы</param>
        public DioramaBlock BlockOf(DioramaDefinition def)
        {
            if (def == null) return null;

            EnsureIndex();
            return blockByDiorama.TryGetValue(def, out var block) ? block : null;
        }

        /// <summary>
        /// Диорамы конкретного блока в порядке их следования
        /// </summary>
        /// <param name="block">Блок</param>
        public IReadOnlyList<DioramaDefinition> InBlock(DioramaBlock block)
        {
            foreach (var entry in blocks)
                if (entry != null && entry.Block == block)
                    return entry.Dioramas;

            return Array.Empty<DioramaDefinition>();
        }

        /// <summary> Сбросить кэш индексов (после изменения списков) </summary>
        public void Invalidate()
        {
            byId = null;
            blockByDiorama = null;
            ordered = null;
        }

        private void EnsureIndex()
        {
            if (byId != null && blockByDiorama != null && ordered != null) return;

            byId = new Dictionary<string, DioramaDefinition>();
            blockByDiorama = new Dictionary<DioramaDefinition, DioramaBlock>();
            ordered = new List<DioramaDefinition>();

            foreach (var entry in blocks)
            {
                if (entry == null) continue;

                foreach (var def in entry.Dioramas)
                {
                    if (def == null) continue;

                    ordered.Add(def);

                    if (!string.IsNullOrEmpty(def.Id))
                        byId[def.Id] = def;

                    if (entry.Block != null)
                        blockByDiorama[def] = entry.Block;
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate() => Invalidate();
#endif
    }
}
