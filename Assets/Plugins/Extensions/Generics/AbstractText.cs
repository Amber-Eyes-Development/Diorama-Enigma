using TMPro;
using UnityEngine;

namespace Extensions.Generics
{
    /// <summary>
    /// Абстракция текста (TMP)
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public abstract class AbstractText : MonoBehaviour
    {
        protected TMP_Text text;

        protected virtual void Awake() => text = GetComponent<TMP_Text>();

        protected virtual void UpdateText(string newText)
        {
            text.text = newText;
        }
    }
}
