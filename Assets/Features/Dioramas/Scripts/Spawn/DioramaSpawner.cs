using System;
using System.Collections.Generic;
using DioramaEnigma.Sequences;
using Extensions.Log;
using Extensions.SceneFlow;
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
    public sealed class DioramaSpawner : MonoBehaviour, ISceneLoadStep
    {
        #region События 
        /// <summary> Фокус сменился: точка кадрирования активной диорамы и нужна ли анимация перехода </summary>
        public event Action<DioramaFocus> onFocusChanged;
        /// <summary> Набор живых инстансов блока изменился (доспавн реальной диорамы / выгрузка блока) </summary>
        public event Action onLiveInstancesChanged;
        #endregion

        #region Свойства 
        /// <summary> Активная (в фокусе) диорама </summary>
        public DioramaDefinition Active => activeDefinition;
        /// <summary> Активный блок </summary>
        public DioramaBlock ActiveBlock => activeBlock;
        /// <summary> Сервис доступа (для потребителей) </summary>
        public DioramaAccessService Access => access;
        /// <summary> Все диорамы активного блока в порядке (вкл. закрытые) </summary>
        public IReadOnlyList<DioramaDefinition> BlockDioramas => queue;
        /// <summary> Живые (реально загруженные) инстансы блока </summary>
        public IReadOnlyCollection<DioramaInstance> LiveInstances => liveInstances.Values;
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
        #endregion
        
        #region Параметры 
        
        [Header("Источник")]
        [Tooltip("Сервис доступа (ассет)")]
        [SerializeField] private DioramaAccessService access;
        [Tooltip("Выбранный блок (ассет-мост из меню)")]
        [SerializeField] private DioramaBlockSelection selection;
        [Tooltip("Канал рантайм-ссылки на спавнер")]
        [SerializeField] private DioramaSpawnerReference reference;
        [Tooltip("Опционально. Координатор готовности сцены: спавнер регистрируется шагом загрузки и удерживает экран до прогрузки диорам блока")]
        [SerializeField] private SceneLoadCoordinator loadCoordinator;

        [Header("Раскладка")]
        [Tooltip("Корень оси: позиция диорамы = axisRoot + spacing * индекс_в_блоке")]
        [SerializeField] private Transform axisRoot;
        [Tooltip("Смещение между соседними диорамами")]
        [SerializeField] private Vector3 spacing = new(40f, 0f, 0f);
        [Tooltip("Заглушка по умолчанию для закрытых диорам (если не задана на самой диораме)")]
        [SerializeField] private GameObject defaultPlaceholder;
        
        #endregion

        #region Переменные 
        
        private readonly List<DioramaDefinition> queue = new();
        private readonly Dictionary<string, DioramaInstance> liveInstances = new();
        private readonly Dictionary<string, GameObject> placeholders = new();
        private readonly HashSet<string> loading = new();

        // Загрузки реальных диорам стартового блока — по ним считается готовность сцены (ISceneLoadStep)
        private readonly HashSet<string> initialLoads = new();
        private int initialLoadTotal;
        private bool blockLoaded;

        private DioramaBlock activeBlock;
        private DioramaDefinition activeDefinition;
        private int activeIndex = -1;
        
        #endregion

        #region ISceneLoadStep 

        // Готовность сцены: блок загружен и все реальные диорамы стартового блока доспавнились
        float ISceneLoadStep.Progress
        {
            get
            {
                if (!blockLoaded) return 0f;
                if (initialLoadTotal == 0) return 1f;

                return Mathf.Clamp01((initialLoadTotal - initialLoads.Count) / (float)initialLoadTotal);
            }
        }

        bool ISceneLoadStep.IsDone => blockLoaded && initialLoads.Count == 0;

        #endregion
        
        #region MonoBehaviour

        private void Start()
        {
            if (access == null)
            {
                ServiceDebug.LogError($"{nameof(access)} не назначен");
                return;
            }

            if (axisRoot == null)
            {
                ServiceDebug.LogError($"{nameof(axisRoot)} не назначен");
                return;
            }

            access.onDioramaUnlocked += OnDioramaUnlocked;

            if (loadCoordinator != null) loadCoordinator.Register(this);

            LoadBlock(ResolveStartBlock());

            var start = ResolveStartDiorama();
            if (start != null)
            {
                activeDefinition = start;
                activeIndex = queue.IndexOf(start);
            }

            UpdateFocusGating();
            EmitFocus(false);
            if (reference != null) reference.Set(this);
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
            
            if (access.IsUnlocked(def)) DioramaProgressStore.SaveLastActive(def.Id);

            UpdateFocusGating();
            EmitFocus(true);
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
            if (selected != null && selected.IsUnlocked) return selected; // устаревший выбор закрытого блока игнорируем

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
            initialLoads.Clear();
            initialLoadTotal = 0;
            blockLoaded = false;
            activeBlock = block;

            if (block == null)
            {
                blockLoaded = true; 
                return;
            }

            queue.AddRange(access.AllInBlock(block));

            for (int i = 0; i < queue.Count; i++)
            {
                var def = queue[i];
                if (def == null) continue;

                if (access.IsUnlocked(def))
                {
                    if (BeginLoadReal(def))
                    {
                        initialLoads.Add(def.Id);
                        initialLoadTotal++;
                    }
                }
                else SpawnPlaceholder(def, i);
            }

            blockLoaded = true;
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

        /// <returns> true, если асинхронная загрузка реально стартовала </returns>
        private bool BeginLoadReal(DioramaDefinition def)
        {
            if (def.RunnerPrefab == null)
            {
                ServiceDebug.LogError($"У диорамы {def.name} не назначен {nameof(def.RunnerPrefab)}");
                return false;
            }

            if (liveInstances.ContainsKey(def.Id) || !loading.Add(def.Id)) return false;

            var operation = InstantiateAsync(def.RunnerPrefab, axisRoot);
            operation.completed += _ => OnRealLoaded(def, operation);
            return true;
        }

        private void OnRealLoaded(DioramaDefinition def, AsyncInstantiateOperation<SequenceRunner> operation)
        {
            loading.Remove(def.Id);
            initialLoads.Remove(def.Id);
            if (this == null) return;

            SequenceRunner runner = operation.Result is { Length: > 0 } ? operation.Result[0] : null;
            if (runner == null) return;

            int index = queue.IndexOf(def);
            if (index < 0 || liveInstances.ContainsKey(def.Id))
            {
                Destroy(runner.gameObject);
                return;
            }

            var instance = runner.GetComponent<DioramaInstance>();
            if (instance == null) instance = runner.gameObject.AddComponent<DioramaInstance>();

            liveInstances[def.Id] = instance;
            runner.transform.position = SlotPosition(index);

            instance.Bind(def, access, runner, this);
            instance.SetFocused(activeDefinition != null && def.Id == activeDefinition.Id);

            onLiveInstancesChanged?.Invoke();
        }

        private void UnloadAll()
        {
            bool hadInstances = liveInstances.Count > 0;

            foreach (var pair in liveInstances)
                if (pair.Value != null) Destroy(pair.Value.gameObject);
            liveInstances.Clear();

            foreach (var pair in placeholders)
                if (pair.Value != null) Destroy(pair.Value);
            placeholders.Clear();

            loading.Clear();

            if (hadInstances) onLiveInstancesChanged?.Invoke();
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
