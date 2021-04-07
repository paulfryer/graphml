using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.NetworkInformation;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace GraphML.Core
{
    public class Graph : IGraph
    {
        public IRecordsProvider RecordsProvider { get; }
        public IFileStore FileStore { get; }

        public Graph(IRecordsProvider recordsProvider, IFileStore fileStore)
        {
            RecordsProvider = recordsProvider;
            FileStore = fileStore;
            EdgeTypes = new List<IEdgeType>();
            NodeTypes = new List<INodeType>();
        }

        public List<IEdgeType> EdgeTypes { get; }
        public List<INodeType> NodeTypes { get; }


        public void AddEdgeType<TFrom, TTo, TSource>(
            Expression<Func<TSource, dynamic>> fromKey,
            Expression<Func<TSource, dynamic>> toKey,
            Func<TSource, bool> predicate = null,
            params Expression<Func<TSource, dynamic>>[] properties)
            where TFrom : class where TTo : class
        {
            var edgeType = new EdgeType<TFrom, TTo, TSource>(fromKey, toKey, predicate, properties);
            if (EdgeTypes.Any(e => e.Label == edgeType.Label))
                throw new Exception($"EdgeType already added: {edgeType.Label}");
            EdgeTypes.Add(edgeType);
        }

        public void AddNodeType<TNode, TSource>(
            Expression<Func<TSource, dynamic>> id,
            Func<TSource, bool> predicate = null,
            params Expression<Func<TSource, dynamic>>[] properties) where TNode : class
        {
            if (NodeTypes.Any(n => n.GetType() == typeof(TNode)))
                throw new Exception($"INodeType already added: {typeof(TNode)}");
            var nodeType = new NodeType<TNode, TSource>(id, predicate, properties);
            NodeTypes.Add(nodeType);
        }


        public async Task SaveEdgesAndNodesToCsv(int parallelLevel = 10)
        {
            var tasks = new List<Task>();
            foreach (var batch in EdgeTypes.Batch(parallelLevel))
            {
                foreach (var edgeType in batch)
                {
                    var method = GetType().GetMethod("SaveEdgesAsCsv");
                    var generic = method.MakeGenericMethod(edgeType.FromType, edgeType.ToType, edgeType.SourceType);
                    tasks.Add(Task.Run(() => (Task)generic.Invoke(this, new object[] { edgeType })));
                }
                await Task.WhenAll(tasks);
            }

            foreach (var batch in NodeTypes.Batch(parallelLevel))
            {
                foreach (var nodeType in batch)
                {
                    var method = GetType().GetMethod("SaveNodesAsCsv");
                    var generic = method.MakeGenericMethod(nodeType.GetType(), nodeType.SourceType);
                    tasks.Add(Task.Run(() => (Task)generic.Invoke(this, new object[] { nodeType })));
                }
                await Task.WhenAll(tasks);
            }
        }

        public async Task SaveEdgesAsCsv<TFrom, TTo, TSource>(
            EdgeType<TFrom, TTo, TSource> edgeType,
            CsvFormat csvFormat = CsvFormat.AutoTrainer)
            where TSource : class
        {
            if (csvFormat != CsvFormat.AutoTrainer)
                throw new NotImplementedException($"Unsupported csv format: {csvFormat}");

            try
            {
                var fromPropertyName = edgeType.From.GetPropertyName();
                var toPropertyName = edgeType.To.GetPropertyName();
                var sb = new StringBuilder();
                sb.Append("~id,~from,~to,~label,~fromLabels,~toLabels");
                if (edgeType.Properties.Any())
                    foreach (var property in edgeType.Properties)
                    {
                        var propertyName = property.GetPropertyName();
                        sb.Append($",{propertyName}");
                    }

                sb.AppendLine();
                var records = await RecordsProvider.GetRecords(edgeType.Predicate);
                foreach (var record in records)
                {
                    var fromValue = edgeType.SourceType.GetProperty(fromPropertyName).GetValue(record);
                    var toValue = edgeType.SourceType.GetProperty(toPropertyName).GetValue(record);

                    if (fromValue == null || toValue == null)
                        continue;

                    var id = $"{edgeType.FromType.Name}:{fromValue}:{edgeType.ToType.Name}:{toValue}".CreateMD5();

                    sb.Append(
                        $"{id},{fromValue},{toValue},{edgeType.Label},{edgeType.FromType.Name},{edgeType.ToType.Name}");

                    if (edgeType.Properties.Any())
                        foreach (var property in edgeType.Properties)
                        {
                            var propertyName = property.GetPropertyName();
                            var value = edgeType.SourceType.GetProperty(propertyName).GetValue(record);
                            sb.Append($",{value}");
                        }

                    sb.AppendLine();
                }

                await FileStore.SaveFile($"{edgeType.Label}.csv", "edges", sb.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        public async Task SaveNodesAsCsv<TNode, TSource>(NodeType<TNode, TSource> nodeType,
            CsvFormat csvFormat = CsvFormat.AutoTrainer)
            where TNode : class
        {
            if (csvFormat != CsvFormat.AutoTrainer)
                throw new NotImplementedException($"Unsupported csv format: {csvFormat}");

            try
            {
                var idName = nodeType.Id.GetPropertyName();
                var propertyTypes = new Dictionary<string, System.Type>();
                if (nodeType.Properties.Any())
                    foreach (var property in nodeType.Properties)
                    {
                        var propertyName = property.GetPropertyName();
                        var propertyType = nodeType.SourceType.GetProperty(propertyName).PropertyType;
                        propertyTypes.Add(propertyName, propertyType);
                    }

                var sb = new StringBuilder();


                sb.Append("~id,~label");
                foreach (var property in propertyTypes)
                    sb.Append($",{property.Key}");
                sb.AppendLine();

                var records = await RecordsProvider.GetRecords(nodeType.Predicate);
                foreach (var record in records)
                {
                    try
                    {
                        var idValue = nodeType.SourceType.GetProperty(idName).GetValue(record);
                        sb.Append($"{idValue},{nodeType.Label}");
                        foreach (var property in propertyTypes)
                        {
                            string propertyValue = null;

                            propertyValue =
                                Convert.ToString(nodeType.SourceType.GetProperty(property.Key).GetValue(record));


                            sb.Append($",{propertyValue}");
                        }
                    }
                    catch (Exception ex1)
                    {
                        Console.WriteLine(ex1.Message);
                    }

                    sb.AppendLine();
                }
                
                await FileStore.SaveFile($"{nodeType.Label}.csv", "nodes", sb.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}