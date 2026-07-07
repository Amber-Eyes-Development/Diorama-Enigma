using System.Collections.Generic;
using UnityEngine;
using Extensions.Log;

namespace Extensions.Identification
{
    /// <summary>
    /// Негенерик-маркер реестра идентифицируемых объектов
    /// </summary>
    /// <remarks>
    /// Существует ради общего кастом-эдитора: <c>CustomEditor</c> нельзя навесить на открытый generic —
    /// инспектор таргетит этот негенерик-базис (с <c>editorForChildClasses</c>), а операции вызывает через
    /// абстрактные методы. Наследоваться напрямую не нужно — используйте <see cref="IdentifiableRegistry{T}"/>
    /// </remarks>
    public abstract class IdentifiableRegistry : ScriptableObject
    {
#if UNITY_EDITOR
        /// <summary>Собрать все подходящие ассеты проекта в реестр (editor)</summary>
        public abstract void CollectAllInEditor();
        /// <summary>Проверить записи и вывести результат в консоль (editor)</summary>
        public abstract void ValidateInEditor();
#endif
    }

    /// <summary>
    /// Базовый реестр идентифицируемых скриптовых объектов
    /// </summary>
    /// <remarks>
    /// Резолв <typeparamref name="T"/> по стабильному строковому <see cref="IdentifiableObject.Id"/> —
    /// заменяет прямые ссылки на ассеты в сохраняемых данных. Наследнику достаточно объявить
    /// <c>[CreateAssetMenu]</c> и указать тип. Передаётся ссылкой (не синглтон)
    /// </remarks>
    /// <typeparam name="T">Тип идентифицируемого объекта реестра</typeparam>
    public abstract class IdentifiableRegistry<T> : IdentifiableRegistry where T : IdentifiableObject
    {
        /// <summary>Все записи реестра</summary>
        public IReadOnlyList<T> Entries => entries;

        [SerializeField] protected List<T> entries = new();

        private Dictionary<string, T> byId;

        /// <summary>Получить запись по идентификатору (или null)</summary>
        public T GetById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            EnsureIndex();
            byId.TryGetValue(id, out T entry);
            return entry;
        }

        /// <summary>Попытаться получить запись по идентификатору</summary>
        public bool TryGetById(string id, out T entry)
        {
            entry = GetById(id);
            return entry != null;
        }

        /// <summary>Идентификатор записи (или null)</summary>
        public string GetId(T entry) => entry == null ? null : entry.Id;

        /// <summary>Случайная запись реестра (или null, если пусто)</summary>
        public T GetRandom()
        {
            if (entries.Count == 0) return null;
            return entries[Random.Range(0, entries.Count)];
        }

        /// <summary>Сбросить кэш-индекс (после изменения списка записей вручную)</summary>
        protected void InvalidateIndex() => byId = null;

        private void EnsureIndex()
        {
            if (byId != null) return;

            byId = new Dictionary<string, T>(entries.Count);
            foreach (T entry in entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.Id)) continue;
                byId[entry.Id] = entry;
            }
        }

#if UNITY_EDITOR
        public override void CollectAllInEditor()
        {
            entries.Clear();

            string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                T entry = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
                if (entry != null) entries.Add(entry);
            }

            InvalidateIndex();
            UnityEditor.EditorUtility.SetDirty(this);
            ServiceDebug.Log($"{GetType().Name}: собрано {entries.Count} записей");
        }

        public override void ValidateInEditor() => ValidateEntries(true);

        protected virtual void OnValidate()
        {
            InvalidateIndex();
            ValidateEntries(false);
        }

        // Проверка записей: пустые/дублирующиеся Id — предупреждениями; logSuccess даёт итоговый лог для ручного Validate
        private void ValidateEntries(bool logSuccess)
        {
            int issues = 0;
            HashSet<string> seen = new HashSet<string>();

            foreach (T entry in entries)
            {
                if (entry == null) continue;

                if (string.IsNullOrEmpty(entry.Id))
                {
                    ServiceDebug.LogWarning($"{GetType().Name}: у записи {entry.name} пустой Id");
                    issues++;
                }
                else if (!seen.Add(entry.Id))
                {
                    ServiceDebug.LogWarning($"{GetType().Name}: дублирующийся Id у записи {entry.name}");
                    issues++;
                }
            }

            if (logSuccess && issues == 0)
                ServiceDebug.Log($"{GetType().Name}: {entries.Count} записей, проблем не найдено");
        }
#endif
    }
}
