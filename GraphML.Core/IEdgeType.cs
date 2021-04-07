using System;

namespace GraphML.Core
{
    public interface IEdgeType
    {
        public string Label { get; }

        public System.Type FromType { get; }

        public System.Type ToType { get; }

        public System.Type SourceType { get; }
    }
}