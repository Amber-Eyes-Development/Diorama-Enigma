using System;
using UnityEngine;

namespace Extensions.EditorTools
{
    /// <summary>
    /// Временно меняет GUI.backgroundColor и гарантированно восстанавливает прежнее значение при выходе из using,
    /// даже если обработчик кнопки внутри блока бросит исключение (иначе цвет "утекает" на остальной IMGUI)
    /// </summary>
    public readonly struct GUIBackgroundColorScope : IDisposable
    {
        private readonly Color _previous;

        public GUIBackgroundColorScope(Color color)
        {
            _previous = GUI.backgroundColor;
            GUI.backgroundColor = color;
        }

        public void Dispose() => GUI.backgroundColor = _previous;
    }
}
