using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace GraphML.Core
{
    public class Graph : IGraph
    {
        public Graph(IRecordsProvider recordsProvider, IFileStore fileStore)
        {
            RecordsProvider = recordsProvider;
            FileStore = fileStore;
            EdgeTypes = new List<IEdgeType>();
            NodeTypes = new List<INodeType>();
            GraphId = $"graph-{DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks:10}";
        }

        public IRecordsProvider RecordsProvider { get; }
        public IFileStore FileStore { get; }

        public List<IEdgeType> EdgeTypes { get; }
        public List<INodeType> NodeTypes { get; }
        public string GraphId { get; }


        public void AddEdgeType<TFrom, TTo>(
            Expression<Func<TFrom, dynamic>> fromKey,
            Expression<Func<TFrom, dynamic>> toKey,
            params Expression<Func<TFrom, dynamic>>[] properties)
            where TFrom : class where TTo : class
        {
            AddEdgeType<TFrom, TTo, TFrom>(fromKey, toKey, null, properties);
        }

        public void AddEdgeType<TFrom, TTo>(
            Expression<Func<TFrom, dynamic>> fromKey,
            Expression<Func<TFrom, dynamic>> toKey,
            Func<TFrom, bool> predicate = null,
            params Expression<Func<TFrom, dynamic>>[] properties)
            where TFrom : class where TTo : class
        {
            AddEdgeType<TFrom, TTo, TFrom>(fromKey, toKey, predicate, properties);
        }

        public void AddEdgeType<TFrom, TTo, TSource>(
            Expression<Func<TSource, dynamic>> fromKey,
            Expression<Func<TSource, dynamic>> toKey,
            params Expression<Func<TSource, dynamic>>[] properties)
            where TFrom : class where TTo : class
        {
            AddEdgeType<TFrom, TTo, TSource>(fromKey, toKey, null, properties);
        }

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

        public void AddNodeType<TNode>(
            Expression<Func<TNode, dynamic>> id,
            Func<TNode, bool> predicate = null,
            params Expression<Func<TNode, dynamic>>[] properties) where TNode : class
        {
            AddNodeType<TNode, TNode>(id, predicate, properties);
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


        public void AddNodeType<TNode>(
            Func<TNode, bool> predicate = null,
            params Expression<Func<TNode, dynamic>>[] properties) where TNode : class
        {
            var id = GetKeyFunction<TNode>();
            if (id == null)
                throw new Exception("Id property is required.");
            AddNodeType<TNode, TNode>(id, predicate, properties);
        }

        public void AddEdgeType<TFrom, TTo>(
            Expression<Func<TFrom, dynamic>> toKey,
            params Expression<Func<TFrom, dynamic>>[] properties)
            where TFrom : class where TTo : class
        {
            if (NodeTypes.All(n => n.NType != typeof(TFrom)) && GetKeyFunction<TFrom>() != null)
                AddNodeType<TFrom>();
            if (NodeTypes.All(n => n.NType != typeof(TTo)) && GetKeyFunction<TTo>() != null)
                AddNodeType<TTo>();

            var fromKey = GetKeyFunction<TFrom>();
            AddEdgeType<TFrom, TTo, TFrom>(fromKey, toKey, null, properties);
        }

        private static Expression<Func<T, dynamic>> GetKeyFunction<T>()
        {
            var nodeType = typeof(T);
            var nodeProperties = nodeType.GetProperties().ToList();
            var keyedProperties = nodeProperties
                .Where(p => p.CustomAttributes.Any(ca => ca.AttributeType.Name == "KeyAttribute"))
                .ToList();
            if (keyedProperties.Count > 1)
                throw new Exception("Only 1 keyed property is supported. Found more than 1.");
            if (keyedProperties.Count == 0)
                return null;
            var keyProperty = keyedProperties.Single();
            var instance = Expression.Parameter(typeof(T), "instance");
            var value = Expression.Property(instance, keyProperty);

            return Expression.Lambda<Func<T, dynamic>>(
               Expression.TypeAs(value, typeof(T)),
                instance);

                /*
                return Expression.Lambda<Func<T, dynamic>>(
                    keyProperty.PropertyType.IsValueType
                        ? Expression.Convert(value, typeof(T))
                        : Expression.TypeAs(value, typeof(T)),
                    instance);*/
        }

        public async Task SaveEdgesAndNodesToCsv(int parallelLevel = 10, CsvFormat csvFormat = CsvFormat.AutoTrainer)
        {
            var tasks = new List<Task>();

            foreach (var batch in NodeTypes.Batch(parallelLevel))
            {
                foreach (var nodeType in batch)
                {
                    var method = GetType().GetMethod("SaveNodesAsCsv");
                    var generic = method.MakeGenericMethod(nodeType.NType, nodeType.SourceType);
                    tasks.Add(Task.Run(() => (Task) generic.Invoke(this, new object[] {nodeType, csvFormat})));
                }

                await Task.WhenAll(tasks);
            }

            foreach (var batch in EdgeTypes.Batch(parallelLevel))
            {
                foreach (var edgeType in batch)
                {
                    var method = GetType().GetMethod("SaveEdgesAsCsv");
                    var generic = method.MakeGenericMethod(edgeType.FromType, edgeType.ToType, edgeType.SourceType);
                    tasks.Add(Task.Run(() => (Task)generic.Invoke(this, new object[] { edgeType, csvFormat })));
                }

                await Task.WhenAll(tasks);
            }
        }


        public async Task SaveEdgesAsCsv<TFrom, TTo, TSource>(
            EdgeType<TFrom, TTo, TSource> edgeType, CsvFormat csvFormat)
            where TSource : class
        {
            try
            {
                var fromPropertyName = edgeType.From.GetPropertyName();
                var toPropertyName = edgeType.To.GetPropertyName();
                var sb = new StringBuilder();
                sb.Append("~id,~from,~to,~label");
                if (csvFormat == CsvFormat.AutoTrainer)
                    sb.Append(",~fromLabels,~toLabels");

                if (edgeType.Properties.Any())
                    foreach (var property in edgeType.Properties)
                    {
                        var propertyName = property.GetPropertyName();
                        sb.Append($",{propertyName}");
                        if (csvFormat == CsvFormat.Neptune)
                        {
                            var propertyTypeName = GetNeptunePropertyTypeName(property.GetPropertyType());
                            sb.Append($":{propertyTypeName}");
                        }
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

                    sb.Append($"{id},{fromValue},{toValue},{edgeType.Label}");

                    if (csvFormat == CsvFormat.AutoTrainer)
                        sb.Append(",{ edgeType.FromType.Name},{ edgeType.ToType.Name}");

                    if (edgeType.Properties.Any())
                        foreach (var property in edgeType.Properties)
                        {
                            var propertyName = property.GetPropertyName();
                            var value = Convert.ToString(edgeType.SourceType.GetProperty(propertyName).GetValue(record));
                            if (csvFormat == CsvFormat.Neptune)
                            {
                                value = GetNeptunePropertyValue(property.GetPropertyType(), value);
                            }

                            if (value != null && value.Contains(","))
                                value = $"\"{value.Replace("\"", "\"\"")}\"";
                            sb.Append($",{value}");
                        }

                    sb.AppendLine();
                }

                await FileStore.SaveFile(GraphId, "edges", $"{edgeType.Label}.csv", sb.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private string GetNeptunePropertyTypeName(Type propertyType)
        {
            return propertyType.Name switch
            {
                "DateTime" => "Date",
                "Int32" => "Int",
                _ => propertyType.Name
            };
        }

        private string GetNeptunePropertyValue(Type propertyType, string value)
        {
            if (propertyType == typeof(DateTime))
            {
                var dateValue = Convert.ToDateTime(value);
                value = dateValue == DateTime.MinValue ? null :
                    dateValue.ToString("yyyy-MM-ddTHH:mm:ssZ");
            }

            return value;
        }

        public async Task SaveNodesAsCsv<TNode, TSource>(NodeType<TNode, TSource> nodeType, CsvFormat csvFormat)
            where TNode : class
            where TSource : class
        {
            try
            {
                var idName = nodeType.Id.GetPropertyName();
                var propertyTypes = new Dictionary<string, Type>();
                if (nodeType.Properties.Any())
                    foreach (var property in nodeType.Properties)
                    {
                        var propertyName = property.GetPropertyName();
                        var propertyType = nodeType.SourceType.GetProperty(propertyName).PropertyType;
                        propertyTypes.Add(propertyName, propertyType);
                    }

                var sb = new StringBuilder();


                sb.Append("~id,~label");

                if (nodeType.Properties.Any())
                    foreach (var property in nodeType.Properties)
                    {
                        var propertyName = property.GetPropertyName();
                        

                        sb.Append($",{propertyName}");
                        
                        if (csvFormat == CsvFormat.Neptune)
                        {
                            var propertyTypeName = GetNeptunePropertyTypeName(property.GetPropertyType());
                            sb.Append($":{propertyTypeName}");
                        }
                    }

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

                            if (csvFormat == CsvFormat.Neptune)
                            {
                                propertyValue = GetNeptunePropertyValue(property.Value, propertyValue);
                            }
                            if (propertyValue != null && propertyValue.Contains(","))
                                propertyValue = $"\"{propertyValue.Replace("\"", "\"\"")}\"";
                            sb.Append($",{propertyValue}");
                        }
                    }
                    catch (Exception ex1)
                    {
                        Console.WriteLine(ex1.Message);
                    }

                    sb.AppendLine();
                }

                await FileStore.SaveFile(GraphId, "nodes", $"{nodeType.Label}.csv", sb.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}