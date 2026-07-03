using Extensions.Data;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Персист выбора игрока (мост меню-сцена): последняя активная диорама и выбранный блок
    /// </summary>
    public static class DioramaProgressStore
    {
        private const string LAST_ACTIVE_KEY = "dioramas.lastActive";
        private const string SELECTED_BLOCK_KEY = "dioramas.selectedBlock";

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
