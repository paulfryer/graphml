using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GraphML.Core
{
    public class InMemoryRecordsProvider : IRecordsProvider
    {
        public IEnumerable<object>[] RecordSources { get; }

        public InMemoryRecordsProvider(params IEnumerable<object>[] recordSources)
        {
            RecordSources = recordSources;
        }

        public async Task<IEnumerable<TSource>> GetRecords<TSource>(Func<TSource, bool> predicate) where TSource : class
        {
            var recordSource = RecordSources.Single(list => list.GetType().GenericTypeArguments[0] == typeof(TSource)).Cast<TSource>();
            return predicate == null ? recordSource : recordSource.Where(predicate);
        }

    
    }
}