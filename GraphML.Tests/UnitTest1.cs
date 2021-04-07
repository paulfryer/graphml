using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GraphML.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GraphML.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public async Task GraphSaves()
        {

            var people = new List<Person>
            {
                new Person {Id = "1", Name = "Person1", FavoritePersonId = "3"},
                new Person {Id = "2", Name = "Person2", FavoritePersonId = "3"},
                new Person{Id = "3", Name = "Person3", FavoritePersonId = "1"}
            };

            var friendRelationships = new List<FriendRelationship>
            {
                new FriendRelationship {SourcePersonId = "1", DestinationPersonId = "2"},
                new FriendRelationship {SourcePersonId = "2", DestinationPersonId = "3"}
            };

            IRecordsProvider recordsProvider = new InMemoryRecordsProvider(people, friendRelationships);
            IFileStore fileStore = new LocalFileStore();
            var graph = new Graph(recordsProvider, fileStore);

            graph.AddEdgeType<Person, Person>(p => p.Id, p => p.FavoritePersonId);
            graph.AddEdgeType<Person, Person, FriendRelationship>(p => p.SourcePersonId, p => p.DestinationPersonId);
            
            graph.AddNodeType<Person>(p => p.Id);

            Assert.IsTrue(graph.NodeTypes.Count == 1);
            Assert.IsTrue(graph.EdgeTypes.Count == 2);

            await graph.SaveEdgesAndNodesToCsv();

            Assert.IsTrue(Directory.Exists("nodes"));
            Assert.IsTrue(Directory.Exists("edges"));
        }


        public class Person
        {
            public string Id { get; set; }

            public string Name { get; set; }

            public string FavoritePersonId { get; set; }
        }


        public class FriendRelationship
        {
            public string SourcePersonId { get; set; }
            public string DestinationPersonId { get; set; }

        }
    }
}
