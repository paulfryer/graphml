using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GraphML.Core
{
    public interface IRecordsProvider
    { 
        Task<IEnumerable<TSource>> GetRecords<TSource>(Func<TSource, bool> predicate)
            where TSource : class;
    }
}