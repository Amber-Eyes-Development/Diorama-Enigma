using UnityEngine;
using Extensions.Data.InMemoryData;

namespace Extensions.EditorTools.Viewpoints
{
    /// <summary>
    /// Структура данных точки вьюпорта SceneNavigatorWindow
    /// </summary>
    [System.Serializable]
    public sealed class ViewpointsData : InMemoryDataEntry
    {
        public string ScenePath;
        public string Name;
        public Vector3 Position;
        public Vector3 Rotation;
        public float Size;
        public bool Orthographic;
        public bool Mode2D;
    }
}