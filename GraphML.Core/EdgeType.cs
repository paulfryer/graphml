using System;
using System.Linq.Expressions;

namespace GraphML.Core
{
    public class EdgeType<TFrom, TTo, TSource> : IEdgeType
    {
        public EdgeType(Expression<Func<TSource, dynamic>> from,
            Expression<Func<TSource, dynamic>> to,
            Func<TSource, bool> predicate = null,
            params Expression<Func<TSource, dynamic>>[] properties)
        {
            From = from;
            To = to;
            Predicate = predicate;
            Properties = properties;
        }

        public Expression<Func<TSource, dynamic>> From { get; }
        public Expression<Func<TSource, dynamic>> To { get; }
        public Func<TSource, bool> Predicate { get; }
        public Expression<Func<TSource, dynamic>>[] Properties { get; }

        public string Label => $"{FromType.Name}_{To.GetPropertyName()}_{ToType.Name}";


        public System.Type FromType => typeof(TFrom);
        public System.Type ToType => typeof(TTo);

        public System.Type SourceType => typeof(TSource);
    }
}