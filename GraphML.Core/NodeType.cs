using System;
using System.Linq.Expressions;

namespace GraphML.Core
{
    public class NodeType<TNode, TSource> : INodeType where TNode : class
    {
        public Expression<Func<TSource, dynamic>> Id;
        public Func<TSource, bool> Predicate;
        public Expression<Func<TSource, dynamic>>[] Properties;

        public NodeType(Expression<Func<TSource, dynamic>> id,
            Func<TSource, bool> predicate = null,
            params Expression<Func<TSource, dynamic>>[] properties)
        {
            Id = id;
            Predicate = predicate;
            Properties = properties;
        }

       public string Label => typeof(TNode).Name;
        public System.Type SourceType => typeof(TSource);
    }
}