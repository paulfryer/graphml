using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphML.Core;
using Microsoft.EntityFrameworkCore;

namespace GraphML.DB
{
    public class DbRecordStore : IRecordsProvider
    {
        public DbRecordStore(DbContext context)
        {
            Context = context;
        }

        public DbContext Context { get; }

        public async Task<IEnumerable<TSource>> GetRecords<TSource>(Func<TSource, bool> predicate) where TSource : class
        {
            return predicate != null
                ? Context.Set<TSource>().Where(predicate)
                : Context.Set<TSource>();
        }
    }
}