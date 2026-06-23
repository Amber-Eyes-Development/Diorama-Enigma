using System;
using System.Collections.Generic;
using DioramaEnigma.Sequences;
using Extensions.Log;
using UnityEngine;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Сессия активного блока на игровой сцене: грузит ВСЕ диорамы выбранного блока (закрытые — заглушками),
    /// раскладывает по слотам, ведёт фокус и навигацию внутри блока
    /// </summary>
    /// <remarks>
    /// Камеру не двигает — шлёт <see cref="onFocusChanged"/>. На открытие диорамы блока меняет её заглушку
    /// на реальную. Публикует себя в канал для динамического UI
    /// </remarks>
    public sealed class DioramaSpawner : MonoBehaviour
    {
        /// <summary> Фокус сменился: точка кадрирования активной диорамы и нужна ли анимация перехода </summary>
        public event Action<DioramaFocus> onFocusChanged;

        /// <summary> Активная (в фокусе) диорама </summary>
        public DioramaDefinition Active => activeDefinition;
        /// <summary> Активный блок </summary>
        public DioramaBlock ActiveBlock => activeBlock;
        /// <summary> Сервис доступа (для потребителей) </summary>
        public DioramaAccessService Access => access;
        /// <summary> Все диорамы активного блока в порядке (вкл. закрытые) </summary>
        public IReadOnlyList<DioramaDefinition> BlockDioramas => queue;
        /// <summary> Есть ли активная диорама </summary>
        public bool HasFocus => activeIndex >= 0;
        /// <summary> Можно ли шагнуть вперёд </summary>
        public bool CanFocusNext => activeIndex >= 0 && activeIndex + 1 < queue.Count;
        /// <summary> Можно ли шагнуть назад </summary>
        public bool CanFocusPrev => activeIndex > 0;
        /// <summary> Точка кадрирования активной диорамы (мировая позиция её слота) </summary>
        public Vector3 ActiveFramePoint => SlotPosition(activeIndex < 0 ? 0 : activeIndex);
        /// <summary> «Домашний» кадр — слот 0; камера берёт своё смещение относительно него </summary>
        public Vector3 HomePoint => axisRoot != null ? axisRoot.position : transform.position;

        [Header("Источник")]
        [Tooltip("Сервис доступа (ассет)")]
        [SerializeField] private DioramaAccessService access;
        [Tooltip("Выбранный блок (ассет-мост из меню)")]
        [SerializeField] private DioramaBlockSelection selection;
        [Tooltip("Канал рантайм-ссылки на спавнер")]
        [SerializeField] private DioramaSpawnerReference reference;

        [Header("Раскладка")]
        [Tooltip("Корень оси: позиция диорамы = axisRoot + spacing * индекс_в_блоке")]
        [SerializeField] private Transform axisRoot;
        [Tooltip("Смещение между соседними диорамами")]
        [SerializeField] private Vector3 spacing = new(40f, 0f, 0f);
        [Tooltip("Заглушка по умолчанию для закрытых диорам (если не задана на самой диораме)")]
        [SerializeField] private GameObject defaultPlaceholder;

        private readonly List<DioramaDefinition> queue = new();
        private readonly Dictionary<string, DioramaInstance> liveInstances = new();
        private readonly Dictionary<string, GameObject> placeholders = new();
        private readonly HashSet<string> loading = new();

        private DioramaBlock activeBlock;
        private DioramaDefinition activeDefinition;
        private int activeIndex = -1;

        #region MonoBehaviour

        private void Start()
        {
            if (access == null)
            {
                ServiceDebug.LogError(this, $"{nameof(access)} не назначен");
                return;
            }

            if (axisRoot == null)
            {
                ServiceDebug.LogError(this, $"{nameof(axisRoot)} не назначен");
                return;
            }

            access.onDioramaUnlocked += OnDioramaUnlocked;
            if (reference != null) reference.Set(this);

            LoadBlock(ResolveStartBlock());

            var start = ResolveStartDiorama();
            if (start != null)
            {
                activeDefinition = start;
                activeIndex = queue.IndexOf(start);
            }

            UpdateFocusGating();
            EmitFocus(false);
        }

        private void OnDestroy()
        {
            if (access != null) access.onDioramaUnlocked -= OnDioramaUnlocked;
            if (reference != null) reference.Clear();
        }

        #endregion

        #region Navigation

        /// <summary> Перейти к следующей диораме блока </summary>
        public void FocusNext()
        {
            if (CanFocusNext) Focus(queue[activeIndex + 1]);
        }

        /// <summary> Перейти к предыдущей диораме блока </summary>
        public void FocusPrev()
        {
            if (CanFocusPrev) Focus(queue[activeIndex - 1]);
        }

        /// <summary> Перейти к конкретной диораме блока </summary>
        /// <param name="def"> Целевая диорама </param>
        public void FocusDiorama(DioramaDefinition def) => Focus(def);

        private void Focus(DioramaDefinition def)
        {
            int index = def != null ? queue.IndexOf(def) : -1;
            if (index < 0) return;

            activeDefinition = def;
            activeIndex = index;

            DioramaProgressStore.SaveLastActive(def.Id);

            UpdateFocusGating();
            EmitFocus(true); // навигация — с анимацией перехода
        }

        // Только активный реальный инстанс принимает ввод
        private void UpdateFocusGating()
        {
            foreach (var pair in liveInstances)
            {
                if (pair.Value == null) continue;
                pair.Value.SetFocused(activeDefinition != null && pair.Key == activeDefinition.Id);
            }
        }

        #endregion

        #region Block load

        private DioramaBlock ResolveStartBlock()
        {
            var selected = access.BlockById(selection != null ? selection.SelectedId : null);
            if (selected != null) return selected;

            var visible = access.VisibleBlocks();
            return visible.Count > 0 ? visible[0] : null;
        }

        private DioramaDefinition ResolveStartDiorama()
        {
            if (queue.Count == 0) return null;

            var last = access.ById(DioramaProgressStore.LoadLastActive());
            if (last != null && queue.Contains(last)) return last;

            foreach (var def in queue)
                if (access.IsUnlocked(def)) return def;

            return queue[0];
        }

        private void LoadBlock(DioramaBlock block)
        {
            UnloadAll();
            queue.Clear();
            activeBlock = block;
            if (block == null) return;

            queue.AddRange(access.AllInBlock(block));

            for (int i = 0; i < queue.Count; i++)
            {
                var def = queue[i];
                if (def == null) continue;

                if (access.IsUnlocked(def)) BeginLoadReal(def);
                else SpawnPlaceholder(def, i);
            }
        }

        private void OnDioramaUnlocked(DioramaDefinition def)
        {
            if (def == null || !queue.Contains(def)) return;
            if (!placeholders.ContainsKey(def.Id)) return;

            DestroyPlaceholder(def.Id);
            BeginLoadReal(def);
        }

        #endregion

        #region Instances

        private void SpawnPlaceholder(DioramaDefinition def, int index)
        {
            var prefab = def.PlaceholderPrefab != null ? def.PlaceholderPrefab : defaultPlaceholder;
            if (prefab == null) return;

            var go = Instantiate(prefab, SlotPosition(index), Quaternion.identity, axisRoot);
            placeholders[def.Id] = go;
        }

        private void DestroyPlaceholder(string id)
        {
            if (placeholders.TryGetValue(id, out var go) && go != null) Destroy(go);
            placeholders.Remove(id);
        }

        private void BeginLoadReal(DioramaDefinition def)
        {
            if (def.RunnerPrefab == null)
            {
                ServiceDebug.LogError(this, $"У диорамы {def.name} не назначен {nameof(def.RunnerPrefab)}");
                return;
            }

            if (liveInstances.ContainsKey(def.Id) || !loading.Add(def.Id)) return;

            var operation = InstantiateAsync(def.RunnerPrefab, axisRoot);
            operation.completed += _ => OnRealLoaded(def, operation);
        }

        private void OnRealLoaded(DioramaDefinition def, AsyncInstantiateOperation<SequenceRunner> operation)
        {
            loading.Remove(def.Id);
            if (this == null) return;

            SequenceRunner runner = operation.Result is { Length: > 0 } ? operation.Result[0] : null;
            if (runner == null) return;

            int index = queue.IndexOf(def);
            if (index < 0 || liveInstances.ContainsKey(def.Id))
            {
                Destroy(runner.gameObject); // блок сменился / уже загружено
                return;
            }

            var instance = runner.GetComponent<DioramaInstance>();
            if (instance == null) instance = runner.gameObject.AddComponent<DioramaInstance>();

            liveInstances[def.Id] = instance;
            runner.transform.position = SlotPosition(index);

            instance.Bind(def, access, runner, this);
            instance.SetFocused(activeDefinition != null && def.Id == activeDefinition.Id);
        }

        private void UnloadAll()
        {
            foreach (var pair in liveInstances)
                if (pair.Value != null) Destroy(pair.Value.gameObject);
            liveInstances.Clear();

            foreach (var pair in placeholders)
                if (pair.Value != null) Destroy(pair.Value);
            placeholders.Clear();

            loading.Clear();
        }

        private Vector3 SlotPosition(int index) => axisRoot.position + spacing * index;

        #endregion

        private void EmitFocus(bool animate)
        {
            if (activeIndex < 0) return;

            onFocusChanged?.Invoke(new DioramaFocus(SlotPosition(activeIndex), animate));
        }
    }
}
