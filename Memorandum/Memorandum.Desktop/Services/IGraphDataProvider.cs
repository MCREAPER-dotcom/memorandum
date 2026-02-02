using System.Collections.Generic;
using Memorandum.Desktop.Models;

namespace Memorandum.Desktop.Services;

public interface IGraphDataProvider
{
    IReadOnlyList<GraphNode> GetNodes();
    IReadOnlyList<GraphEdge> GetEdges();
}
