using System;
using UnityEngine;
using Extensions.Data.InMemoryData;

namespace Extensions.EditorTools.Viewpoints
{
    /// <summary>
    /// Область видимости точки обзора EditorViewpointsWindow
    /// </summary>
    [Serializable]
    public enum ViewpointScope
    {
        /// <summary>Частная: видна только на сцене, где сохранена</summary>
        Scene,
        /// <summary>Общая: видна на любой сцене, не привязана к ней</summary>
        Global
    }

    /// <summary>
    /// Структура данных точки обзора EditorViewpointsWindow
    /// </summary>
    /// <remarks>
    /// Хранит ориентир камеры SceneView (pivot + поворот), а не позицию самой камеры:
    /// позиция вычисляется из pivot и дистанции, иначе при перспективе восстановление уводит камеру по вектору вида
    /// </remarks>
    [Serializable]
    public sealed class ViewpointsData : InMemoryDataEntry
    {
        public string Name;
        public ViewpointScope Scope;

        // Заполняется только для Scope == Scene
        public string ScenePath;

        public Vector3 Pivot;
        public Quaternion Rotation;
        public float Size;
        public bool Orthographic;
        public bool Mode2D;
    }
}
