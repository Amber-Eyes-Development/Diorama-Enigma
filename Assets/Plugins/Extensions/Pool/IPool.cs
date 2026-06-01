using UnityEngine;

namespace Extensions.Pool
{
    /// <summary>
    /// Интерфейс базового пула
    /// </summary>
    public interface IPool
    {
        Component Get(Transform parent);
        void Release(Component instance);
        bool Owns(Component instance);
    }
}