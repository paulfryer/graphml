using System;

namespace GraphML.Core
{
    public interface IEdgeType
    {
        public string Label { get; }

        public Type FromType { get; }

        public Type ToType { get; }

        public Type SourceType { get; }
    }
}