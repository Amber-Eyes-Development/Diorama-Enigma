using System.Collections.Generic;

namespace DioramaEnigma.Dioramas
{
    /// <summary>
    /// Срез карты диорам для визуализации: узлы (с состоянием) и рёбра связей
    /// </summary>
    public sealed class DioramaMapView
    {
        /// <summary> Узлы (все диорамы реестра) </summary>
        public IReadOnlyList<DioramaNode> Nodes { get; }
        /// <summary> Рёбра (связи между диорамами) </summary>
        public IReadOnlyList<DioramaEdge> Edges { get; }

        public DioramaMapView(IReadOnlyList<DioramaNode> nodes, IReadOnlyList<DioramaEdge> edges)
        {
            Nodes = nodes;
            Edges = edges;
        }
    }
}
