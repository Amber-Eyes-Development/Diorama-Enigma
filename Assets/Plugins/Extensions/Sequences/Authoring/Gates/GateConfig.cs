using System;

namespace Extensions.Sequences
{
    /// <summary>
    /// Авторский конфиг гейта (инлайн, полиморфно через [SerializeReference]) → строит рантайм-<see cref="Gate"/>
    /// </summary>
    [Serializable]
    public abstract class GateConfig
    {
        /// <summary> Построить рантайм-гейт </summary>
        public abstract Gate Build(BuildContext context);
    }
}
