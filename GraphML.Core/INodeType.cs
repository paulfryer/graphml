using System;

namespace GraphML.Core
{
    public interface INodeType
    {
        Type NodeType { get; }

        Type SourceType { get; }
    }
}