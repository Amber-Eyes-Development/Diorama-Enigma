using System;

namespace Extensions.Sequences
{
    /// <summary>
    /// Авторский конфиг эффекта (инлайн, полиморфно через [SerializeReference]) → строит рантайм-<see cref="Effect"/>
    /// </summary>
    [Serializable]
    public abstract class EffectConfig
    {
        /// <summary> Построить рантайм-эффект </summary>
        public abstract Effect Build(BuildContext context);
    }
}
