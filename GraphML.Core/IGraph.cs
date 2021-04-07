using System;
using System.Collections.Generic;

namespace GraphML.Core
{
    public interface IGraph
    {
        List<IEdgeType> EdgeTypes { get;  }
        List<INodeType> NodeTypes { get; }

        string GraphId { get; }

    }
}