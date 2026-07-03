using System.Collections.Generic;
using UnityEngine;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Реестр диорам: упорядоченный список блоков, каждый со своими записями диорам
    /// </summary>
    /// <remarks>
    /// Единый источник истины для очерёдности, принадлежности к блокам и параметров доступа
    /// </remarks>
    [CreateAssetMenu(menuName = "Dioramas/Registry", fileName = nameof(DioramaRegistry))]
    public sealed class DioramaRegistry : ScriptableObject
    {
        /// <summary> Записи блоков в порядке прохождения </summary>
        public IReadOnlyList<DioramaBlockEntry> Blocks => blocks;

        [SerializeField] private List<DioramaBlockEntry> blocks = new();

        private Dictionary<string, DioramaDefinition> byId;
        private Dictionary<string, DioramaBlock> blockById;
        private Dictionary<DioramaDefinition, DioramaBlock> blockByDiorama;
        private List<DioramaEntry> entries;

        /// <summary>
        /// Все записи диорам по порядку: блок за блоком, внутри блока — по списку
        /// </summary>
        public IReadOnlyList<DioramaEntry> Entries()
        {
            EnsureIndex();
            return entries;
        }

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
        /// Блок по идентификатору (или null)
        /// </summary>
        /// <param name="id">Идентификатор блока</param>
        public DioramaBlock BlockById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            EnsureIndex();
            return blockById.TryGetValue(id, out var block) ? block : null;
        }

        /// <summary> Сбросить кэш индексов (после изменения списков) </summary>
        public void Invalidate()
        {
            byId = null;
            blockById = null;
            blockByDiorama = null;
            entries = null;
        }

        private void EnsureIndex()
        {
            if (byId != null && blockById != null && blockByDiorama != null && entries != null) return;

            byId = new Dictionary<string, DioramaDefinition>();
            blockById = new Dictionary<string, DioramaBlock>();
            blockByDiorama = new Dictionary<DioramaDefinition, DioramaBlock>();
            entries = new List<DioramaEntry>();

            foreach (var blockEntry in blocks)
            {
                if (blockEntry == null) continue;

                var block = blockEntry.Block;
                if (block != null && !string.IsNullOrEmpty(block.Id)) blockById[block.Id] = block;

                foreach (var entry in blockEntry.Dioramas)
                {
                    if (entry?.Definition == null) continue;

                    entries.Add(entry);

                    var def = entry.Definition;
                    if (!string.IsNullOrEmpty(def.Id)) byId[def.Id] = def;
                    if (block != null) blockByDiorama[def] = block;
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate() => Invalidate();
#endif
    }
}
