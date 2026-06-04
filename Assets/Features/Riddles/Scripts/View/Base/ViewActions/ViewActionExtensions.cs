namespace DioramaEnigma.Riddles
{
    /// <summary>
    /// Хелперы применения списков действий
    /// </summary>
    public static class ViewActionExtensions
    {
        /// <summary> Выполнить все действия над частицами </summary>
        public static void Apply(this ParticleSystemAction[] actions, bool silent)
        {
            if (actions == null) return;

            foreach (var action in actions)
                action.Apply(silent);
        }

        /// <summary> Применить все переключения активности </summary>
        public static void Apply(this GameObjectActivation[] activations)
        {
            if (activations == null) return;

            foreach (var activation in activations)
                activation.Apply();
        }
    }
}
