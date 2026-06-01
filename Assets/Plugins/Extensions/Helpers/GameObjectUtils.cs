using System.Collections.Generic;
using UnityEngine;

namespace Extensions.Helpers
{
    /// <summary>
    /// Утилиты для работы с <see cref="GameObject"/>
    /// </summary>
    public static class GameObjectUtils
    {
        /// <summary>
        /// Очистить детей объекта
        /// </summary>
        /// <param name="target">Целевой объект</param>
        public static void ClearChildren(Transform target)
        {
            if (target == null) return;

            List<GameObject> children = new List<GameObject>();

            foreach (Transform child in target)
                children.Add(child.gameObject);

            foreach (GameObject child in children)
                Object.Destroy(child);
        }
    }
}