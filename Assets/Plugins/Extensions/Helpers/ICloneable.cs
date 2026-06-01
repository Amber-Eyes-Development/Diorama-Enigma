namespace Extensions.Helpers
{
    /// <summary>
    /// Клониуремый объект определенного типа
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ICloneable<T>
    {
        /// <summary>
        /// Глубокая копия
        /// </summary>
        public T CloneDeep();
        
        /// <summary>
        /// Поверхностная копия
        /// </summary>
        public T CloneShallow();
    }
}