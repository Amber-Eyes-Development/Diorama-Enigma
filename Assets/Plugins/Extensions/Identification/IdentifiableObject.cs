using Extensions.Helpers;
using UnityEngine;

namespace Extensions.Identification
{
    /// <summary>
    /// Базовый идентифицируемый скриптовый объект
    /// </summary>
    public abstract class IdentifiableObject : ScriptableObject
    {
        /// <summary>
        /// Идентификатор
        /// </summary>
        public string Id => id;

        [Header("Идентификация"), Space]
        [SerializeField]
        protected string id;

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (!string.IsNullOrEmpty(id))
                return;

            id = IdGenerator.NewGuid();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}