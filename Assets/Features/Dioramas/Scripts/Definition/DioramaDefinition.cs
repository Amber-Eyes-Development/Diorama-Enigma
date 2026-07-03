using DioramaEnigma.Sequences;
using Extensions.Data;
using UnityEngine;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Определение диорамы
    /// </summary>
    [CreateAssetMenu(menuName = "Dioramas/Diorama", fileName = nameof(DioramaDefinition))]
    public sealed class DioramaDefinition : DescribedAsset
    {
        private const string UNLOCK_KEY_PREFIX = "diorama.unlock:";
        
        /// <summary> Префаб диорамы (раннер в корне) — инстанцируется на сцену </summary>
        public SequenceRunner RunnerPrefab => runnerPrefab;
        /// <summary> Последовательность диорамы (та же, что у раннера в префабе) </summary>
        public Sequence Sequence => sequence;
        /// <summary> Модель-заглушка, пока диорама закрыта (если пусто — общая заглушка спавнера) </summary>
        public GameObject PlaceholderPrefab => placeholderPrefab;

        /// <summary>
        /// Полностью ли пройдены ВСЕ шаги (вкл. Always/фоновые) — трек №1
        /// </summary>
        public bool IsFullyCompleted => sequence != null && sequence.IsCompleted;

        /// <summary>
        /// Решена ли диорама: пройдены все линейные группы (Always не учитываются) — трек №2
        /// </summary>
        public bool IsSolved => sequence != null && sequence.IsSolved;

        /// <summary> Разблокирована ли диорама </summary>
        public bool IsUnlocked
        {
            get
            {
                LoadUnlockIfNeeded();
                return unlocked;
            }
        }

        [Header("Диорама"), Space]
        [Tooltip("Префаб диорамы — в корне должен быть SequenceRunner")]
        [SerializeField] private SequenceRunner runnerPrefab;
        [Tooltip("Последовательность диорамы (та же, что назначена раннеру в префабе)")]
        [SerializeField] private Sequence sequence;
        [Tooltip("Модель-заглушка, пока диорама закрыта (опционально; иначе общая заглушка спавнера)")]
        [SerializeField] private GameObject placeholderPrefab;

        [System.NonSerialized] private bool unlocked;
        private bool unlockLoaded;

        private string UnlockKey => UNLOCK_KEY_PREFIX + Id;

        private void OnEnable()
        {
            unlockLoaded = false;
            unlocked = false;
        }

        /// <summary> Разблокировать диораму (идемпотентно, персистит) </summary>
        public void Unlock() => SetUnlocked(true);

        /// <summary> Снять разблокировку (для рестарта блока / нового прогресса) </summary>
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
