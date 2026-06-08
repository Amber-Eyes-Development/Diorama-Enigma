using UnityEngine;

namespace DioramaEnigma.Sequences
{
    /// <summary>
    /// Поле-ссылка (или массив ссылок) на ассет, обязанный реализовывать <see cref="ISequenceStep"/>.
    /// В инспекторе отклоняет назначение объектов, не являющихся шагами.
    /// </summary>
    public sealed class SequenceStepReferenceAttribute : PropertyAttribute { }
}
