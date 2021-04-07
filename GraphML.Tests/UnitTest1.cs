using System.Collections.Generic;
using GraphML.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GraphML.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {

            var people = new List<Person>
            {
                new Person {Id = "1", Name = "Person1"},
                new Person {Id = "2", Name = "Person2"},
                new Person{Id = "3", Name = "Person3"}
            };

            var friendRelationships = new List<FriendRelationship>
            {
                new FriendRelationship {SourcePersonId = "1", DestinationPersonId = "2"},
                new FriendRelationship {SourcePersonId = "2", DestinationPersonId = "3"}
            };

            IRecordsProvider recordsProvider = new InMemoryRecordsProvider(people, friendRelationships);
            IFileStore fileStore = new LocalFileStore();
            var graph = new Graph(recordsProvider, fileStore);

            graph.AddEdgeType<Person, Person, FriendRelationship>(p => p.SourcePersonId, p => p.DestinationPersonId);


        }


        public class Person
        {
            public string Id { get; set; }

            public string Name { get; set; }
        }


        public class FriendRelationship
        {
            public string SourcePersonId { get; set; }
            public string DestinationPersonId { get; set; }

        }
    }
}
