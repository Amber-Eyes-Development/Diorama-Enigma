using Extensions.Data;
using UnityEngine;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Блок — тематический набор диорам (группа/ассет-метка)
    /// </summary>
    [CreateAssetMenu(menuName = "Dioramas/Block", fileName = nameof(DioramaBlock))]
    public sealed class DioramaBlock : DescribedAsset
    {
        /// <summary> Префаб ручного UI-макета карты блока (комнаты-кнопки + двери) — для окна-карты </summary>
        public GameObject MapLayoutPrefab => mapLayoutPrefab;

        /// <summary> Разблокирован ли блок (персистентный флаг прогресса) </summary>
        public bool IsUnlocked
        {
            get
            {
                LoadUnlockIfNeeded();
                return unlocked;
            }
        }

        [Header("Карта блока")]
        [Tooltip("Префаб вручную собранной карты блока (комнаты-кнопки + двери)")]
        [SerializeField] private GameObject mapLayoutPrefab;

        [System.NonSerialized] private bool unlocked;
        private bool unlockLoaded;

        private string UnlockKey => "block.unlock:" + Id;

        private void OnEnable()
        {
            unlockLoaded = false;
            unlocked = false;
        }

        /// <summary> Разблокировать блок </summary>
        public void Unlock() => SetUnlocked(true);

        /// <summary> Снять разблокировку (для нового прогресса) </summary>
        public void LockReset() => SetUnlocked(false);

        private void SetUnlocked(bool value)
        {
            LoadUnlockIfNeeded();
            if (unlocked == value) return;

            unlocked = value;
            if (Application.isPlaying) JsonSaveLoad.Save(unlocked, UnlockKey);
        }

        private void LoadUnlockIfNeeded()
        {
            if (!Application.isPlaying || unlockLoaded) return;

            unlockLoaded = true;
            unlocked = JsonSaveLoad.Load(UnlockKey, false);
        }
    }
}
