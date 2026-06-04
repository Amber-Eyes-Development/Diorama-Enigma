using System;
using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using Extensions.Log;
using Extensions.Pool;
using UnityEngine;

namespace DioramaEnigma.Tactility
{
    /// <summary>
    /// Пул worker-объектов <see cref="DOTweenAnimation"/> для воспроизведения тактильных анимаций
    /// </summary>
    public sealed class ClickAnimationPool : MonoBehaviour
    {
        // Поля цели и рантайм-состояния выставляются вручную при ретаргете, копировать их не нужно
        private static readonly HashSet<string> ExcludedFields = new()
        {
            "tween", "target", "targetGO", "targetIsSelf", "tweenTargetIsTargetGO"
        };

        private static FieldInfo[] copyFields;

        [Tooltip("Префаб worker'а: GameObject с одним DOTweenAnimation (autoGenerate/autoPlay/autoKill выключены)")]
        [SerializeField] private DOTweenAnimation workerPrefab;

        [Tooltip("Сколько worker'ов создать заранее")]
        [Min(0)]
        [SerializeField] private int prewarm = 4;

        [Tooltip("Максимальный размер пула (0 — без ограничений)")]
        [Min(0)]
        [SerializeField] private int maxSize = 16;

        private ObjectPool<DOTweenAnimation> pool;

        private void Awake()
        {
            if (workerPrefab == null)
            {
                ServiceDebug.LogWarning(this, "workerPrefab не назначен, анимации клика отключены");
                return;
            }

            pool = new ObjectPool<DOTweenAnimation>(workerPrefab, transform, prewarm, maxSize);
        }

        /// <summary>
        /// Проиграть анимацию по шаблону на указанной цели
        /// </summary>
        /// <param name="template">Референсный шаблон (набор параметров)</param>
        /// <param name="target">Цель применения анимации</param>
        public void Play(DOTweenAnimation template, Transform target)
        {
            if (pool == null || template == null || target == null) return;
            if (!template.isValid)
            {
                ServiceDebug.LogWarning(this, "Шаблон анимации не валиден, анимация не проиграна");
                return;
            }

            DOTweenAnimation worker = pool.Get();
            if (worker == null) return;

            CopyParameters(template, worker);
            Retarget(worker, template, target);

            worker.CreateTween(regenerateIfExists: true, andPlay: true);

            if (worker.tween == null)
            {
                pool.Release(worker);
                return;
            }

            worker.tween.OnComplete(() => pool.Release(worker));
        }

        #region Internal

        private static void CopyParameters(DOTweenAnimation from, DOTweenAnimation to)
        {
            FieldInfo[] fields = GetCopyFields();
            foreach (FieldInfo field in fields)
                field.SetValue(to, field.GetValue(from));
        }

        private static void Retarget(DOTweenAnimation worker, DOTweenAnimation template, Transform target)
        {
            worker.targetIsSelf = false;
            worker.tweenTargetIsTargetGO = true;
            worker.targetGO = target.gameObject;

            // Подбираем компонент того же типа, что и у шаблона (Transform для скейла/панча/шейка и т.п.)
            Type targetComponentType = template.target != null ? template.target.GetType() : typeof(Transform);
            Component resolved = target.GetComponent(targetComponentType);
            worker.target = resolved != null ? resolved : target;
        }

        private static FieldInfo[] GetCopyFields()
        {
            if (copyFields != null) return copyFields;

            FieldInfo[] all = typeof(DOTweenAnimation).GetFields(BindingFlags.Public | BindingFlags.Instance);
            List<FieldInfo> filtered = new(all.Length);
            foreach (FieldInfo field in all)
                if (!ExcludedFields.Contains(field.Name))
                    filtered.Add(field);

            copyFields = filtered.ToArray();
            return copyFields;
        }

        #endregion
    }
}
