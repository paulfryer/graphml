using System;

namespace GraphML.Core
{
    public interface INodeType
    {
        string Label { get; }

        Type SourceType { get; }
    }
}