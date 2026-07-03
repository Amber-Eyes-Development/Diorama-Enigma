using System;
using UnityEngine;

namespace Extensions.Sequences
{
    /// <summary>
    /// Конфиг эффекта задания состояния булева шага
    /// </summary>
    [Serializable]
    public sealed class SetStepEffectConfig : EffectConfig
    {
        [Tooltip("Шаг, состояние которого нужно изменить")]
        [SerializeField] private BoolStepDefinition target;
        [Tooltip("Устанавливаемое значение")]
        [SerializeField] private bool value = true;

        /// <inheritdoc/>
        public override Effect Build(BuildContext context)
        {
            var step = context.GetOrBuild(target) as BoolStep;
            return new SetStepEffect(step, value);
        }
    }
}
