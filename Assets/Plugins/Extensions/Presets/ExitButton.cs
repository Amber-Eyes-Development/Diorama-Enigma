using Extensions.Generics;
using UnityEngine;

namespace Extensions.Presets
{
    /// <summary>
    /// Кнопка закрытия программы
    /// </summary>
    public class ExitButton : AbstractButtonAction
    {
        public override void OnButtonClickAction() => Quit();
        
        private void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}