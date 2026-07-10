using DioramaEnigma.Dioramas;
using DioramaEnigma.Sequences;

namespace DioramaEnigma.Quests
{
    /// <summary>
    /// Read-model одного видимого квеста шага <see cref="SequenceStep"/>
    /// </summary>
    /// <remarks>
    /// Ключ слота — <see cref="StepId"/>; <see cref="Diorama"/> нужен для фокус-арбитража панели
    /// </remarks>
    public readonly struct ActiveQuest
    {
        /// <summary> Id шага-источника (ключ для диффа слотов) </summary>
        public string StepId { get; }
        /// <summary> Текст задания </summary>
        public string Text { get; }
        /// <summary> Приоритетный ли квест </summary>
        public bool Priority { get; }
        /// <summary> Диорама, которой принадлежит квест </summary>
        public DioramaDefinition Diorama { get; }
        /// <summary> Шаг выполнен, но слот удержан до приоритетного впереди (показать метку «готово», не убирать) </summary>
        public bool Done { get; }

        public ActiveQuest(string stepId, string text, bool priority, DioramaDefinition diorama, bool done = false)
        {
            StepId = stepId;
            Text = text;
            Priority = priority;
            Diorama = diorama;
            Done = done;
        }
    }
}
