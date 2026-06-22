using System;
using System.Collections.Generic;
using DioramaEnigma.Sequences;
using Extensions.Log;
using UnityEngine;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Динамический спавнер диорам на игровой сцене
    /// </summary>
    /// <remarks>
    /// Держит окно из 2*radius+1 инстансов вокруг активной вдоль оси (упреждающая подгрузка InstantiateAsync,
    /// выгрузка вышедших из окна), ведёт очередь и фокус, позиционирует инстансы по индексу в очереди
    /// </remarks>
    public sealed class DioramaSpawner : MonoBehaviour
    {
        /// <summary> Фокус сменился: точка кадрирования активной диорамы и нужна ли анимация перехода </summary>
        public event Action<DioramaFocus> onFocusChanged;

        /// <summary> Активная (в фокусе) диорама </summary>
        public DioramaDefinition Active => activeDefinition;
        /// <summary> Сервис доступа (для потребителей канала спавнера) </summary>
        public DioramaAccessService Access => access;
        /// <summary> Есть ли активная диорама </summary>
        public bool HasFocus => activeIndex >= 0;
        /// <summary> Точка кадрирования активной диорамы (мировая позиция её слота) </summary>
        public Vector3 ActiveFramePoint => SlotPosition(activeIndex < 0 ? 0 : activeIndex);
        /// <summary> «Домашний» кадр — слот 0; камера берёт своё смещение относительно него </summary>
        public Vector3 HomePoint => axisRoot != null ? axisRoot.position : transform.position;

        [Header("Источник")]
        [Tooltip("Бутстраппер с сервисом доступа")]
        [SerializeField] private DioramaBootstrapper bootstrapper;
        [Tooltip("Канал рантайм-ссылки на спавнер")]
        [SerializeField] private DioramaSpawnerReference reference;

        [Header("Раскладка")]
        [Tooltip("Корень оси: позиция диорамы = axisRoot + spacing * индекс_в_очереди")]
        [SerializeField] private Transform axisRoot;
        [Tooltip("Смещение между соседними диорамами")]
        [SerializeField] private Vector3 spacing = new(40f, 0f, 0f);
        [Tooltip("Радиус окна: одновременно загружено 2*radius+1 диорам")]
        [Min(0)]
        [SerializeField] private int windowRadius = 1;

        private readonly List<DioramaDefinition> queue = new();
        private readonly Dictionary<string, DioramaInstance> live = new();
        private readonly HashSet<string> loading = new();

        private DioramaAccessService access;
        private DioramaDefinition activeDefinition;
        private int activeIndex = -1;

        #region MonoBehaviour

        private void Start()
        {
            if (bootstrapper == null)
            {
                ServiceDebug.LogError($"{nameof(bootstrapper)} не назначен");
                return;
            }

            if (axisRoot == null)
            {
                ServiceDebug.LogError($"{nameof(axisRoot)} не назначен");
                return;
            }

            access = bootstrapper.Initialize();
            if (access == null) return;

            access.onDioramaUnlocked += OnDioramaUnlocked;

            if (reference != null) reference.Set(this);

            queue.Clear();
            queue.AddRange(access.VisibleOrdered());

            var start = ResolveStartDiorama();
            if (start != null)
            {
                activeDefinition = start;
                activeIndex = queue.IndexOf(start);
            }

            ReconcileWindow();
            EmitFocus(false);
        }

        private void OnDestroy()
        {
            if (access != null) access.onDioramaUnlocked -= OnDioramaUnlocked;
            if (reference != null) reference.Clear();
        }

        #endregion

        #region Navigation

        /// <summary> Перейти к следующей диораме в очереди </summary>
        public void FocusNext()
        {
            if (activeIndex + 1 < queue.Count) Focus(queue[activeIndex + 1]);
        }

        /// <summary> Перейти к предыдущей диораме в очереди </summary>
        public void FocusPrev()
        {
            if (activeIndex - 1 >= 0) Focus(queue[activeIndex - 1]);
        }

        /// <summary> Перейти к конкретной диораме (если открыта) </summary>
        /// <param name="def"> Целевая диорама </param>
        public void FocusDiorama(DioramaDefinition def) => Focus(def);

        private void Focus(DioramaDefinition def)
        {
            int index = def != null ? queue.IndexOf(def) : -1;
            if (index < 0) return;

            activeDefinition = def;
            activeIndex = index;

            DioramaProgressStore.SaveLastActive(def.Id);

            ReconcileWindow();
            EmitFocus(true); 
        }

        #endregion

        #region Queue / window

        private void OnDioramaUnlocked(DioramaDefinition def) => RebuildQueue();

        private void RebuildQueue()
        {
            queue.Clear();
            queue.AddRange(access.VisibleOrdered());

            if (activeDefinition != null && queue.Contains(activeDefinition))
            {
                activeIndex = queue.IndexOf(activeDefinition);
            }
            else
            {
                activeIndex = queue.Count > 0 ? 0 : -1;
                activeDefinition = activeIndex >= 0 ? queue[0] : null;
            }

            ReconcileWindow();
            EmitFocus(false); 
        }

        private DioramaDefinition ResolveStartDiorama()
        {
            string savedId = DioramaProgressStore.LoadLastActive();
            if (!string.IsNullOrEmpty(savedId))
            {
                var saved = access.ById(savedId);
                if (saved != null && queue.Contains(saved)) return saved;
            }

            return queue.Count > 0 ? queue[0] : null;
        }

        private void ReconcileWindow()
        {
            if (queue.Count == 0)
            {
                UnloadAll();
                return;
            }

            int lo = Mathf.Max(0, activeIndex - windowRadius);
            int hi = Mathf.Min(queue.Count - 1, activeIndex + windowRadius);

            UnloadOutside(lo, hi);

            for (int i = lo; i <= hi; i++)
            {
                var def = queue[i];
                if (def == null) continue;

                if (live.TryGetValue(def.Id, out var instance))
                {
                    instance.transform.position = SlotPosition(i);
                    instance.SetFocused(i == activeIndex);
                }
                else if (!loading.Contains(def.Id))
                {
                    BeginLoad(def);
                }
            }
        }

        private void UnloadOutside(int lo, int hi)
        {
            var toRemove = new List<string>();

            foreach (var pair in live)
            {
                int index = pair.Value != null ? queue.IndexOf(pair.Value.Definition) : -1;
                if (index < lo || index > hi) toRemove.Add(pair.Key);
            }

            foreach (var id in toRemove) Unload(id);
        }

        private void UnloadAll()
        {
            var ids = new List<string>(live.Keys);
            foreach (var id in ids) Unload(id);
        }

        private void Unload(string id)
        {
            if (live.TryGetValue(id, out var instance) && instance != null)
                Destroy(instance.gameObject);

            live.Remove(id);
        }

        #endregion

        #region Loading

        private void BeginLoad(DioramaDefinition def)
        {
            if (def.RunnerPrefab == null)
            {
                ServiceDebug.LogError($"У диорамы {def.name} не назначен {nameof(def.RunnerPrefab)}");
                return;
            }

            loading.Add(def.Id);

            var operation = InstantiateAsync(def.RunnerPrefab, axisRoot);
            operation.completed += _ => OnLoaded(def, operation);
        }

        private void OnLoaded(DioramaDefinition def, AsyncInstantiateOperation<SequenceRunner> operation)
        {
            loading.Remove(def.Id);
            if (this == null) return;

            SequenceRunner runner = operation.Result != null && operation.Result.Length > 0 ? operation.Result[0] : null;
            if (runner == null) return;

            int index = queue.IndexOf(def);
            bool wanted = index >= 0 && IsInWindow(index) && !live.ContainsKey(def.Id);
            if (!wanted)
            {
                Destroy(runner.gameObject);
                return;
            }

            var instance = runner.GetComponent<DioramaInstance>();
            if (instance == null) instance = runner.gameObject.AddComponent<DioramaInstance>();

            live[def.Id] = instance;

            runner.transform.position = SlotPosition(index);

            instance.Bind(def, access, runner, this);
            instance.SetFocused(index == activeIndex);
        }

        private bool IsInWindow(int index) =>
            index >= activeIndex - windowRadius && index <= activeIndex + windowRadius;

        private Vector3 SlotPosition(int index) => axisRoot.position + spacing * index;

        #endregion

        private void EmitFocus(bool animate)
        {
            if (activeIndex < 0) return;

            onFocusChanged?.Invoke(new DioramaFocus(SlotPosition(activeIndex), animate));
        }
    }
}
