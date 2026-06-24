using System.Collections.Generic;
using Extensions.Data;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Персист прогресса диорам: когда-либо пройденные (страховка поверх состояния шагов) и последняя активная.
    /// Единственное место, знающее о ключах и <see cref="JsonSaveLoad"/>; хранится per-profile
    /// </summary>
    public static class DioramaProgressStore
    {
        private const string COMPLETED_KEY = "dioramas.completed";
        private const string LAST_ACTIVE_KEY = "dioramas.lastActive";
        private const string SELECTED_BLOCK_KEY = "dioramas.selectedBlock";

        /// <summary> Идентификаторы когда-либо пройденных диорам </summary>
        public static List<string> LoadCompleted() => JsonSaveLoad.Load(COMPLETED_KEY, new List<string>());

        /// <summary> Сохранить идентификаторы когда-либо пройденных диорам </summary>
        /// <param name="completedIds">Идентификаторы пройденных диорам</param>
        public static void SaveCompleted(IEnumerable<string> completedIds) =>
            JsonSaveLoad.Save(new List<string>(completedIds), COMPLETED_KEY);

        /// <summary> Id последней активной диорамы (или null) </summary>
        public static string LoadLastActive() => JsonSaveLoad.Load<string>(LAST_ACTIVE_KEY, null);

        /// <summary> Сохранить Id последней активной диорамы </summary>
        /// <param name="dioramaId">Идентификатор диорамы</param>
        public static void SaveLastActive(string dioramaId) => JsonSaveLoad.Save(dioramaId, LAST_ACTIVE_KEY);

        /// <summary> Id выбранного в меню блока (или null) </summary>
        public static string LoadSelectedBlock() => JsonSaveLoad.Load<string>(SELECTED_BLOCK_KEY, null);

        /// <summary> Сохранить Id выбранного блока </summary>
        /// <param name="blockId">Идентификатор блока</param>
        public static void SaveSelectedBlock(string blockId) => JsonSaveLoad.Save(blockId, SELECTED_BLOCK_KEY);
    }
}
