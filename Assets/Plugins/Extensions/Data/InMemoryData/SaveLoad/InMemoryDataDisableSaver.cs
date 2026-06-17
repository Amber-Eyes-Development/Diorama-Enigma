using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Extensions.Log;
using UnityEngine;

namespace Extensions.Data.InMemoryData
{
    /// <summary>
    /// Сохранение InMemory БД на OnDisable
    /// </summary>
    /// <remarks>
    /// Для БД с отключённым автосейвом. При выключении объекта данные синхронно помечаются к записи;
    /// фактическую (батч-)запись на диск ведёт JsonSaveLoad. Гарантию записи при выходе даёт
    /// <see cref="InMemoryDataPauseSaver"/> (через Application.wantsToQuit).
    /// </remarks>
    public class InMemoryDataDisableSaver : MonoBehaviour
    {
        [SerializeField] protected List<InMemoryDataBaseObject> dataBases = new();

        [Tooltip("Показывать лог сохранения")]
        [SerializeField] protected bool showSaveLog = false;

        protected virtual void OnDisable() => SaveAllManual();

        /// <summary>
        /// Сохранить все БД: синхронно помечает данные к записи, запись на диск — батчем через JsonSaveLoad
        /// </summary>
        public void SaveAllManual()
        {
            int requested = 0;
            foreach (InMemoryDataBaseObject dataBase in dataBases)
            {
                if (dataBase == null) continue;

                dataBase.RequestSave();
                requested++;
            }

            // Поторопить запись батча; гарантию записи при выходе/потере фокуса даёт JsonSaveLoad / PauseSaver
            JsonSaveLoad.FlushAsync().Forget();

            if (showSaveLog) ServiceDebug.Log($"Помечено к сохранению {requested} БД");
        }

        /// <summary>
        /// Сохранить все БД и дождаться фактической записи на диск
        /// </summary>
        /// <returns>Количество сохранённых БД</returns>
        public async UniTask<int> SaveAllManualAsync()
        {
            int savedCount = 0;
            foreach (InMemoryDataBaseObject dataBase in dataBases)
            {
                if (dataBase == null) continue;

                if (await dataBase.RequestSaveAsync()) savedCount++;
            }

            return savedCount;
        }
    }
}
