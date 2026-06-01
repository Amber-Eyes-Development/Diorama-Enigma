using System;
using UnityEngine;
using UnityEngine.UI;

namespace Extensions.Generics
{
    public enum ToggleGroupMode
    {
        Radio = 0,
        Range = 1,
        Multi = 2
    }

    /// <summary>
    /// Группа тогглов
    /// </summary>
    /// <remarks>
    /// Управляет тогглами группы в различных режимах
    /// </remarks>
    public class ToggleGroupControl : MonoBehaviour
    {
        /// <summary>
        /// Событие измненеия состояния группы
        /// </summary>
        public event Action<int> OnValueChanged;

        [SerializeField]
        protected Toggle[] toggles = default;
        [SerializeField]
        protected ToggleGroupMode mode = ToggleGroupMode.Radio;
        [SerializeField]
        protected bool allowEmptySelection = true;

        protected bool suppressCallback;

        protected virtual void Awake()
        {
            if (toggles == null)
            {
                toggles = Array.Empty<Toggle>();
            }
        }

        protected virtual void OnEnable()
        {
            // Подписка на изменение состояния тогглов
            for (int i = 0; i < toggles.Length; i++)
            {
                Toggle toggle = toggles[i];
                if (toggle == null)
                {
                    continue;
                }

                int index = i;
                toggle.onValueChanged.AddListener(value => OnToggleChanged(index, value));
            }
        }

        protected virtual void OnDisable()
        {
            // Отписка от изменения состояния тогглов
            for (int i = 0; i < toggles.Length; i++)
            {
                Toggle toggle = toggles[i];
                if (toggle == null)
                {
                    continue;
                }

                toggle.onValueChanged.RemoveAllListeners();
            }
        }

        protected virtual void OnToggleChanged(int index, bool state)
        {
            if (suppressCallback)
            {
                return;
            }

            if (mode == ToggleGroupMode.Multi)
            {
                RaiseChanged(GetActiveCount());
                return;
            }

            if (mode == ToggleGroupMode.Radio)
            {
                int selectedIndex = state ? index : -1;

                if (!allowEmptySelection && selectedIndex < 0)
                {
                    SetRadio(index);
                    return;
                }

                if (selectedIndex >= 0)
                {
                    SetRadio(selectedIndex);
                }
                else
                {
                    SetAll(false);
                }

                RaiseChanged(GetRadioIndex());
                return;
            }

            if (mode == ToggleGroupMode.Range)
            {
                int value = state ? index + 1 : index;

                if (!allowEmptySelection && value == 0)
                {
                    value = 1;
                }

                SetRange(value);
                RaiseChanged(GetActiveCount());
            }
        }

        /// <summary>
        /// Назначить режим работы группы
        /// </summary>
        public void SetMode(ToggleGroupMode newMode)
        {
            mode = newMode;
            Refresh();
        }

        /// <summary>
        /// Принудительно обновить состояние, не меняя выбранные тогглы
        /// </summary>
        public void Refresh()
        {
            suppressCallback = true;

            if (mode == ToggleGroupMode.Radio)
            {
                int idx = GetRadioIndex();
                if (!allowEmptySelection && idx < 0 && toggles.Length > 0)
                {
                    SetRadioInternal(0);
                }
                else
                {
                    SetRadioInternal(idx);
                }
            }
            else if (mode == ToggleGroupMode.Range)
            {
                int count = GetActiveCount();
                if (!allowEmptySelection && count == 0 && toggles.Length > 0)
                {
                    SetRangeInternal(1);
                }
                else
                {
                    SetRangeInternal(count);
                }
            }

            suppressCallback = false;
        }

        /// <summary>
        /// Выбрать один Toggle по индексу (Radio)
        /// </summary>
        public void SetRadio(int index)
        {
            if (mode != ToggleGroupMode.Radio)
            {
                return;
            }

            SetRadioInternal(index);
            RaiseChanged(GetRadioIndex());
        }

        /// <summary>
        /// Выставить "лесенку" из N включённых Toggle (Range)
        /// </summary>
        public void SetRange(int count)
        {
            if (mode != ToggleGroupMode.Range)
            {
                return;
            }

            count = Mathf.Clamp(count, 0, toggles.Length);

            if (!allowEmptySelection && count == 0 && toggles.Length > 0)
            {
                count = 1;
            }

            SetRangeInternal(count);
            RaiseChanged(GetActiveCount());
        }

        /// <summary>
        /// Включить или выключить все Toggle (Multi/служебно)
        /// </summary>
        public void SetAll(bool state)
        {
            suppressCallback = true;

            for (int i = 0; i < toggles.Length; i++)
            {
                Toggle toggle = toggles[i];
                if (toggle == null)
                {
                    continue;
                }

                toggle.isOn = state;
            }

            suppressCallback = false;

            if (!state && !allowEmptySelection)
            {
                Refresh();
                return;
            }

            RaiseChanged(GetActiveCount());
        }

        /// <summary>
        /// Получить количество включённых Toggle
        /// </summary>
        public int GetActiveCount()
        {
            int count = 0;

            for (int i = 0; i < toggles.Length; i++)
            {
                Toggle toggle = toggles[i];
                if (toggle == null)
                {
                    continue;
                }

                if (toggle.isOn)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Получить индекс выбранного Toggle (Radio), либо -1 если не выбран
        /// </summary>
        public int GetRadioIndex()
        {
            for (int i = 0; i < toggles.Length; i++)
            {
                Toggle toggle = toggles[i];
                if (toggle == null)
                {
                    continue;
                }

                if (toggle.isOn)
                {
                    return i;
                }
            }

            return -1;
        }

        protected void SetRadioInternal(int index)
        {
            suppressCallback = true;

            for (int i = 0; i < toggles.Length; i++)
            {
                Toggle toggle = toggles[i];
                if (toggle == null)
                {
                    continue;
                }

                toggle.isOn = (i == index) && index >= 0;
            }

            suppressCallback = false;
        }

        protected void SetRangeInternal(int count)
        {
            suppressCallback = true;

            for (int i = 0; i < toggles.Length; i++)
            {
                Toggle toggle = toggles[i];
                if (toggle == null)
                {
                    continue;
                }

                toggle.isOn = i < count;
            }

            suppressCallback = false;
        }

        protected void RaiseChanged(int value)
        {
            if (OnValueChanged == null)
            {
                return;
            }

            OnValueChanged.Invoke(value);
        }
    }
}
