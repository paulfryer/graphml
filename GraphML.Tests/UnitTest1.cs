using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;
using GraphML.Core;
using GraphML.DB;
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
                new Person {Id = "1", Name = "Person1", FavoritePersonId = "3", NickName = "Wheels", Age = 25, AverageIncome = 80000, HighSchoolGraduationDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(1000))},
                new Person {Id = "2", Name = "Person2", NickName = "Baskets", FavoritePersonId = "3", Age = 35, AverageIncome = 125000},
                new Person{Id = "3", Name = "Person3", FavoritePersonId = "1", Age = 45, AverageIncome = 150000, HighSchoolGraduationDate = DateTime.Now.Subtract(TimeSpan.FromDays(5000))}
            };

            var friendRelationships = new List<FriendRelationship>
            {
                new FriendRelationship {SourcePersonId = "1", DestinationPersonId = "2", DateMet = DateTime.Now.Subtract(TimeSpan.FromDays(100))},
                new FriendRelationship {SourcePersonId = "2", DestinationPersonId = "3", DateMet = DateTime.Now.Subtract(TimeSpan.FromDays(10))}
            };

            IRecordsProvider recordsProvider = new InMemoryRecordsProvider(people, friendRelationships);
            IFileStore fileStore = new LocalFileStore();
            var graph = new Graph(recordsProvider, fileStore);


            graph.AddNodeType<Person>(p => p.Id, null, p => p.Age, p => p.AverageIncome, 
                p => p.HighSchoolGraduationDate, p => p.NickName);

            graph.AddEdgeType<Person, Person>(p => p.FavoritePersonId);
            graph.AddEdgeType<Person, Person, FriendRelationship>(p => p.SourcePersonId, p => p.DestinationPersonId, p => p.DateMet);
            
            Assert.IsTrue(graph.NodeTypes.Count == 1);
            Assert.IsTrue(graph.EdgeTypes.Count == 2);

            await graph.SaveEdgesAndNodesToCsv(10, CsvFormat.Neptune);

            Assert.IsTrue(Directory.Exists(graph.GraphId));


        }


        public class Person
        {
            [Key]
            public string Id { get; set; }

            public string Name { get; set; }

            public string NickName { get; set; }

            public string FavoritePersonId { get; set; }

            public int Age { get; set; }

            public double AverageIncome { get; set; }

            public DateTime HighSchoolGraduationDate { get; set; }
        }


        public class FriendRelationship
        {
            public string SourcePersonId { get; set; }
            public string DestinationPersonId { get; set; }

            public DateTime DateMet { get; set; }
        }
    }
}
