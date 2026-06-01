using UnityEngine;

namespace Extensions.Pool
{
    /// <summary>
    /// Метка для опционального контроллера пулов
    /// </summary>
    /// <remarks>
    /// Нужна исключительно для сценария работы с PoolsController
    /// Автоматически добавляется при спавне контроллером
    /// </remarks>
    public class PooledObject : MonoBehaviour
    {
        /// <summary>
        /// InstanceID runtime-инстанса объекта (префаба)
        /// </summary>
        public int PrefabId => prefabId;
        /// <summary>
        /// Хозяин объекта
        /// </summary>
        public PoolsController Owner => owner;

        protected int prefabId;
        protected PoolsController owner;

        /// <summary>
        /// Инициализация данных олъекта
        /// </summary>
        /// <param name="owner">Хоязяин (контроллер пулов)</param>
        /// <param name="prefabId">InstanceID runtime-инстанса объекта (префаба)</param>
        public void Initialize(PoolsController owner, int prefabId)
        {
            this.owner = owner;
            this.prefabId = prefabId;
        }

        /// <summary>
        /// Очистить данные о хозяине
        /// </summary>
        /// <returns></returns>
        public bool TryReleaseToOwner()
        {
            if (owner == null)
            {
                return false;
            }

            owner.Despawn(this);
            return true;
        }
    }
}