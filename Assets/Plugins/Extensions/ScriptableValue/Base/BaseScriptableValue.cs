using Extensions.Identification;

namespace Extensions.ScriptableValues
{
    /// <summary>
    /// Базовая абстракция ScriptableValue без указания типа
    /// </summary>
    public abstract class BaseScriptableValue : IdentifiableObject
    {
        protected const string GLOBAL_PROFILE = "global values";
        
        /// <summary>
        /// Сброс значения к дефолтному
        /// </summary>
        public abstract void ResetToDefault();
        
        /// <summary>
        /// Очистка значения
        /// </summary>
        public abstract void Clear();
        
    }
}