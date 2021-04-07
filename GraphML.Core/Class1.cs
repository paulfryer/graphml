using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace GraphML.Core
{
    public class PredictInput
    {
        public List<string> vertices = new List<string>();

        public string property { get; set; }

        public int topk { get; set; }
    }


    public class Graph<TDbContext, TPredictNode, TPredictSource> where TDbContext : DbContext, new() where TPredictNode : class, IKeyed where TPredictSource : class
    {
        public List<IEdgeType> Edges = new List<IEdgeType>();
        public List<INodeType> Nodes = new List<INodeType>();

        public NodeType<TPredictNode, TPredictSource> PredictNodeType { get; set; }




        public string GraphId { get; set; }
        public string Bucket { get; set; }
        public string Prefix { get; set; }
        public Expression<Func<TPredictSource, dynamic>> PredictValue { get; }
        public IAmazonS3 S3 { get; }

        public void AddEdge<TFrom, TTo>(
            Expression<Func<TFrom, dynamic>> toKey,
            params Expression<Func<TFrom, dynamic>>[] properties) where TFrom : class, IKeyed where TTo : class, IKeyed
        {

            if (typeof(ICoded).IsAssignableFrom(typeof(TFrom)))
            {
                Expression<Func<TFrom, dynamic>> fromKey = f => ((ICoded)f).Code;
                AddEdge<TFrom, TTo>(fromKey, toKey, null, properties);
            }
            else if (typeof(IId).IsAssignableFrom(typeof(TFrom)))
            {
                Expression<Func<TFrom, dynamic>> fromKey = f => ((IId)f).Id;
                AddEdge<TFrom, TTo>(fromKey, toKey, null, properties);
            }
            else throw new NotImplementedException();

        }

                public void AddNode<TNode, TSource>(
            Expression<Func<TSource, dynamic>> id,
            params Expression<Func<TSource, dynamic>>[] properties) where TNode : class
        {
            AddNode<TNode, TSource>(id, null, properties);
        }

        public void AddEdge<TFrom, TTo>(
            Expression<Func<TFrom, dynamic>> fromKey,
            Expression<Func<TFrom, dynamic>> toKey,
            params Expression<Func<TFrom, dynamic>>[] properties) where TFrom : class, IKeyed where TTo : class, IKeyed
        {
            AddEdge<TFrom, TTo>(fromKey, toKey, null, properties);
        }

        public void AddEdge<TFrom, TTo>(
            Expression<Func<TFrom, dynamic>> fromKey,
            Expression<Func<TFrom, dynamic>> toKey,
            Func<TFrom, bool> predicate = null,
            params Expression<Func<TFrom, dynamic>>[] properties) where TFrom : class, IKeyed where TTo : class, IKeyed
        {
            var edge = new EdgeType<TFrom, TTo, TFrom>(fromKey, toKey, predicate, properties);
            Console.WriteLine($"Adding edgeType: {edge.Label}");
            Edges.Add(edge);
            if (!Nodes.Any(n => n.NodeType == typeof(TFrom)))
                AddNode<TFrom>();
            if (!Nodes.Any(n => n.NodeType == typeof(TTo)))
                AddNode<TTo>();
        }

        public void AddEdge<TFrom, TTo, TSource>(
            Expression<Func<TSource, dynamic>> fromKey,
            Expression<Func<TSource, dynamic>> toKey,
            params Expression<Func<TSource, dynamic>>[] properties)
            where TFrom : class, IKeyed where TTo : class, IKeyed
        {
            AddEdge<TFrom, TTo, TSource>(fromKey, toKey, null, properties);
        }

        public void AddEdge<TFrom, TTo, TSource>(
            Expression<Func<TSource, dynamic>> fromKey,
            Expression<Func<TSource, dynamic>> toKey,
            Func<TSource, bool> predicate = null,
            params Expression<Func<TSource, dynamic>>[] properties)
            where TFrom : class, IKeyed where TTo : class, IKeyed
        {
            var edge = new EdgeType<TFrom, TTo, TSource>(fromKey, toKey, predicate, properties);

            Console.WriteLine($"Adding edgeType: {edge.Label}");

            Edges.Add(edge);
            if (!Nodes.Any(n => n.NodeType == typeof(TFrom)))
                AddNode<TFrom>();
            if (!Nodes.Any(n => n.NodeType == typeof(TTo)))
                AddNode<TTo>();


        }




        public void AddNode<TNode>(
            Expression<Func<TNode, dynamic>> id,
            params Expression<Func<TNode, dynamic>>[] properties) where TNode : class
        {
            AddNode<TNode, TNode>(id, null, properties);
        }

        public void AddNode<TNode>() where TNode : class, IKeyed
        {


            if (typeof(ICoded).IsAssignableFrom(typeof(TNode)))
            {
                Expression<Func<TNode, dynamic>> id = f => ((ICoded)f).Code;
                AddNode<TNode, TNode>(id);
            }
            else if (typeof(IId).IsAssignableFrom(typeof(TNode)))
            {
                Expression<Func<TNode, dynamic>> id = f => ((IId)f).Id;
                AddNode<TNode, TNode>(id);
            }
            else throw new NotImplementedException();

        }





        public async Task SavePredictsInput<TNode, TSource>(NodeType<TNode, TSource> nodeType) where TNode : class where TSource : class
        {
            var nodeType = node.NodeType;

            var idName = node.Id.GetPropertyName();
            var propertyTypes = new Dictionary<string, Type>();
            if (node.Properties.Any())
                foreach (var property in node.Properties)
                {
                    var propertyName = property.GetPropertyName();
                    var propertyType = nodeType.GetProperty(propertyName).PropertyType;
                    propertyTypes.Add(propertyName, propertyType);
                }


            await using var db = new TDbContext();
            var records = node.Predicate != null
                ? db.Set<TSource>().Where(node.Predicate).ToList()
                : db.Set<TSource>().ToList();

            var batchIndex = 0;
            foreach (var batch in records.Batch(500))
            {
                var input = new PredictInput
                {
                    topk = 100,
                    property = PredictValue.GetPropertyName()
                };

                foreach (var record in batch.ToList())
                {
                    var idValue = (string)nodeType.GetProperty(idName).GetValue(record);
                    input.vertices.Add(idValue);
                }

                await S3.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = Bucket,
                    Key = $"{Prefix}/predict/input/records-{batchIndex}.json",
                    ContentType = "application/json",
                    ContentBody = JsonConvert.SerializeObject(input, Formatting.Indented)
                });

                batchIndex++;
            }
        }


        public async Task SaveNode<TNode, TSource>(NodeType<TNode, TSource> nodeType) where TNode : class, IKeyed where TSource : class
        {
            try
            {

                var startTime = DateTime.Now;
                Console.WriteLine($"Saving nodeType: {nodeType.NodeType.Name}");


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
                foreach (var property in propertyTypes)
                    sb.Append($",{property.Key}");
                sb.AppendLine();
                await using (var db = new TDbContext())
                {


                    var records = nodeType.Predicate != null
                        ? db.Set<TSource>().Where(nodeType.Predicate).OrderBy(nodeType.Id.Compile())
                        : db.Set<TSource>().OrderBy(nodeType.Id.Compile());

                    foreach (var record in records)
                    {
                        try
                        {
                            var idValue = nodeType.SourceType.GetProperty(idName).GetValue(record);
                            sb.Append($"{idValue},{nodeType.NodeType.Name}");
                            foreach (var property in propertyTypes)
                            {
                                string propertyValue = null;

                                propertyValue = Convert.ToString(nodeType.SourceType.GetProperty(property.Key).GetValue(record));


                                sb.Append($",{propertyValue}");
                            }
                        }
                        catch (Exception ex1)
                        {
                            Console.WriteLine(ex1.Message);
                        }

                        sb.AppendLine();
                    }
                }

                Console.WriteLine($"Done saving nodeType: {nodeType.NodeType.Name}, total time: {DateTime.Now - startTime}");

                await S3.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = Bucket,
                    Key = $"{Prefix}/nodes/{nodeType.NodeType.Name}.csv",
                    ContentType = "text/csv",
                    ContentBody = sb.ToString()
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }


        }


        private string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using var md5 = MD5.Create();
            var inputBytes = Encoding.ASCII.GetBytes(input);
            var hashBytes = md5.ComputeHash(inputBytes);

            // Convert the byte array to hexadecimal string
            var sb = new StringBuilder();
            for (var i = 0; i < hashBytes.Length; i++) sb.Append(hashBytes[i].ToString("X2"));
            return sb.ToString();
        }

        public async Task BuildModel()
        {
            var config = new Config();
            var trainingConfig = new TrainingJobConfig();


            foreach (var node in Nodes)
            {
                var method = GetType().GetMethod("ToNodeGraphItem");
                var generic = method.MakeGenericMethod(node.NodeType, node.SourceType);
                var result = (GraphItem)generic.Invoke(this, new object[] { node });
                trainingConfig.Graph.Add(result);

                var method1 = GetType().GetMethod("ToNodeConfig");
                var generic1 = method1.MakeGenericMethod(node.NodeType, node.SourceType);
                var result1 = (NodeConfig)generic1.Invoke(this, new object[] { node });
                config.Nodes.Add(result1);
            }

            foreach (var edge in Edges)
            {
                var method = GetType().GetMethod("ToEdgeGraphItem");
                var generic = method.MakeGenericMethod(edge.FromType, edge.ToType, edge.SourceType);
                var result = (GraphItem)generic.Invoke(this, new object[] { edge });
                trainingConfig.Graph.Add(result);

                var method1 = GetType().GetMethod("ToEdgeConfig");
                var generic1 = method1.MakeGenericMethod(edge.FromType, edge.ToType, edge.SourceType);
                var result1 = (EdgeConfig)generic1.Invoke(this, new object[] { edge });
                config.Edges.Add(result1);
            }


            var configJson = JsonConvert.SerializeObject(config, Formatting.Indented);
            var trainingConfigJson = JsonConvert.SerializeObject(trainingConfig, Formatting.Indented,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });



            var tasks = new List<Task>
            {
                SavePredictsInput(PredictNodeType),
                S3.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = Bucket,
                    Key = $"{Prefix}/config.json",
                    ContentType = "application/json",
                    ContentBody = configJson
                }),

                S3.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = Bucket,
                    Key = $"{Prefix}/training-job-configuration.json",
                    ContentType = "application/json",
                    ContentBody = trainingConfigJson
                })
            };



            foreach (var batch in Edges.Batch(10))
            {
                foreach (var edge in batch)
                {
                    var method = GetType().GetMethod("SaveEdge");
                    var generic = method.MakeGenericMethod(edge.FromType, edge.ToType, edge.SourceType);
                    tasks.Add(Task.Run(() => (Task)generic.Invoke(this, new object[] { edge })));
                    //tasks.Add(task);
                }

                await Task.WhenAll(tasks);
            }

            foreach (var batch in Nodes.Batch(10))
            {
                foreach (var node in batch)
                {
                    var method = GetType().GetMethod("SaveNode");
                    var generic = method.MakeGenericMethod(node.NodeType, node.SourceType);
                    tasks.Add(Task.Run(() => (Task)generic.Invoke(this, new object[] { node })));
                    // tasks.Add(task);
                }

                await Task.WhenAll(tasks);
            }

        }

        public GraphItem ToEdgeGraphItem<TFrom, TTo, TSource>(EdgeType<TFrom, TTo, TSource> edgeType)
        {
            if (edgeType.Properties.Any())
            {
                var featureGraphItem = new FeatureGraphItem
                {
                    FileName = $"edges/{edgeType.Label}.csv",
                    Features = new List<EdgeFeature>()
                };
                // Treat these as features.
                foreach (var property in edgeType.Properties)
                {
                    var propertyName = property.GetPropertyName();
                    var predictName = PredictValue.GetPropertyName();
                    if (propertyName == predictName)
                    {
                        featureGraphItem.Labels ??= new List<EdgeLabel>();

                        featureGraphItem.Labels.Add(new EdgeLabel
                        {
                            Columns = new List<string> { "~id", property.Name },
                            EdgeType = edgeType.Label,
                            LabelType = "edgeType",
                            SubLabelType = "edge_class_label",
                            SplitRate = new AutoConstructedList<double> { 0.7, 0.1, 0.2 }
                        });
                    }
                    else
                    {
                        var edgeFeature = new EdgeFeature
                        {
                            FeatureType = "edgeType",
                            Columns = new List<string>
                            {
                                "~from", "~to", propertyName
                            },
                            EdgeType = new List<string>
                            {
                                edgeType.FromType.Name, edgeType.Label, edgeType.ToType.Name
                            }
                        };

                        var propertyType = edgeType.SourceType.GetProperty(propertyName).PropertyType;
                        if (propertyType == typeof(string))
                        {
                            edgeFeature.SubFeatureType = "category";
                        }
                        else // we assume this is a numeric.
                        {
                            edgeFeature.SubFeatureType = "numerical";
                            edgeFeature.Norm = "min-max";
                        }

                        featureGraphItem.Features.Add(edgeFeature);
                    }
                }

                return featureGraphItem;
            }

            var edgeGraphItem = new EdgeGraphItem
            {
                FileName = $"edges/{edgeType.Label}.csv",
                Edges = new List<EdgeDetail>()
            };
            // Treat this as an edgeType.
            edgeGraphItem.Edges.Add(new EdgeDetail
            {
                EdgeSpecType = "edgeType",
                Columns = new List<string> { "~from", "~to" },
                EdgeType = new List<string>
                {
                    edgeType.FromType.Name, edgeType.Label, edgeType.ToType.Name
                }
            });

            return edgeGraphItem;
        }

        public NodeGraphItem ToNodeGraphItem<TNode, TSource>(NodeType<TNode, TSource> nodeType) where TNode : class
        {
            var nodeGraphItem = new NodeGraphItem
            {
                FileName = $"nodes/{nodeType.NodeType.Name}.csv"
            };

            foreach (var property in nodeType.Properties)
            {
                var propertyName = property.GetPropertyName();
                var predictName = PredictValue.GetPropertyName();
                if (propertyName == predictName)
                {
                    nodeGraphItem.Labels = new List<NodeLabel>
                    {
                        new NodeLabel
                        {
                            NodeType = nodeType.NodeType.Name,
                            LabelType = "nodeType",
                            SubLabelType =
                                "node_class_label", // TODO: change this to allow for other types of sublabels, like regresision. Look at the type of the property to determine 
                            Columns = new List<string> {"~id", propertyName},
                            SplitRate = new List<double> {0.7, 0.1, 0.2},
                            Separator = ";"
                        }
                    };
                }
                else
                {
                    nodeGraphItem.Features ??= new List<NodeFeature>();

                    var feature = new NodeFeature
                    {
                        Columns = new List<string> { "~id", propertyName },
                        FeatureType = "nodeType",
                        NodeType = nodeType.NodeType.Name
                    };

                    if (nodeType.SourceType.GetProperty(propertyName).PropertyType == typeof(string))
                    {
                        feature.SubFeatureType = "category";
                    }
                    else // we assume everything else is numerical.
                    {
                        feature.SubFeatureType = "numerical";
                        feature.Norm = "min-max";
                    }

                    nodeGraphItem.Features.Add(feature);
                }
            }

            return nodeGraphItem;
        }


        public NodeConfig ToNodeConfig<TNode, TSource>(NodeType<TNode, TSource> nodeType) where TNode : class
        {
            var nodeConfig = new NodeConfig
            {
                Label = new NodeLabelConfig
                {
                    Label = nodeType.NodeType.Name
                }
            };

            foreach (var property in nodeType.Properties)
            {
                var propertyName = property.GetPropertyName();
                var propertyType = nodeType.SourceType.GetProperty(propertyName).PropertyType;

                var propConf = new PropertyConfig
                {
                    Property = propertyName,
                    IsNullable = Nullable.GetUnderlyingType(propertyType) != null,
                    IsMultiValue = false
                };
                var underlyingType = Nullable.GetUnderlyingType(propertyType);
                propConf.DataType = propConf.IsNullable ? underlyingType.Name : propertyType.Name;

                if (propConf.DataType.StartsWith("Int"))
                    propConf.DataType = "Integer";



                nodeConfig.Properties.Add(propConf);


            }

            return nodeConfig;
        }

        public EdgeConfig ToEdgeConfig<TFrom, TTo, TSource>(EdgeType<TFrom, TTo, TSource> edgeType)
        {
            var edgeConfig = new EdgeConfig
            {
                Label = new EdgeLabelConfig
                {
                    Label = edgeType.Label,
                    FromLabels = new List<string>
                    {
                        edgeType.FromType.Name
                    },
                    ToLabels = new List<string>
                    {
                        edgeType.ToType.Name
                    }
                }
            };

            foreach (var property in edgeType.Properties)
            {
                var propertyName = property.GetPropertyName();
                var propertyType = edgeType.SourceType.GetProperty(propertyName).PropertyType;

                var edgeProp = new PropertyConfig
                {
                    Property = propertyName,
                    IsNullable = Nullable.GetUnderlyingType(propertyType) != null,
                    IsMultiValue = false
                };

                var underlyingType = Nullable.GetUnderlyingType(propertyType);

                edgeProp.DataType = edgeProp.IsNullable ? underlyingType.Name : propertyType.Name;
                if (edgeProp.DataType.StartsWith("Int"))
                    edgeProp.DataType = "Integer";

                edgeConfig.Properties.Add(edgeProp);
            }

            return edgeConfig;
        }
    }




}
