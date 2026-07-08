namespace DioramaEnigma.Inventory
{
    /// <summary> Поверхность панели инвентаря для реакций объектов сцены </summary>
    public interface IInventoryView
    {
        /// <summary> Отметить/снять, что предмет нужен для наведённого объекта </summary>
        void SetItemNeeded(ResourceValue item, bool needed);
    }
}
