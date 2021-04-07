using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GraphML.Core
{
    public interface IGraphRecordsProvider 
    {
        Task<IEnumerable<TSource>> GetEdges<TSource>(Func<TSource, bool> predicate);

        Task<IEnumerable<TSource>> GetNodes<TSource>(Func<TSource, bool> predicate);
    }
}