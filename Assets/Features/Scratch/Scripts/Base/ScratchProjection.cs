namespace DioramaEnigma.Scratch
{
    /// <summary>
    /// Способ перевода точки курсора в координату маски стирания
    /// </summary>
    public enum ScratchProjection
    {
        /// <summary> Проекция мировой точки в бокс декали-проектора (плоско) </summary>
        DecalProjector,
        /// <summary> Реальный UV поверхности из рейкаста (меш + MeshCollider) </summary>
        SurfaceUv,
    }
}
