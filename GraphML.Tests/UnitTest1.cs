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
                new Person {Id = 1, Name = "Person1", CityCode = "SEA", FavoritePersonId = 3, NickName = "Wheels", Age = 25, AverageIncome = 80000, HighSchoolGraduationDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(1000))},
                new Person {Id = 2, Name = "Person2", CityCode = "SFO", NickName = "Baskets", FavoritePersonId = 3, Age = 35, AverageIncome = 125000},
                new Person{Id = 3, Name = "Person3", CityCode = "LAX", FavoritePersonId = 1, Age = 45, AverageIncome = 150000, HighSchoolGraduationDate = DateTime.Now.Subtract(TimeSpan.FromDays(5000))}
            };

            var friendRelationships = new List<FriendRelationship>
            {
                new FriendRelationship {SourcePersonId = 1, DestinationPersonId = 2, DateMet = DateTime.Now.Subtract(TimeSpan.FromDays(100))},
                new FriendRelationship {SourcePersonId = 2, DestinationPersonId =3, DateMet = DateTime.Now.Subtract(TimeSpan.FromDays(10))}
            };

            var cities = new List<City>
            {
                new City {Code = "SEA", Name = "Seattle, WA"},
                new City {Code = "NYC", Name = "New\" York City"},
                new City {Code = "LAX", Name = "Los Angeles"},
                new City {Code = "SFO", Name = "San Francisco"}
            };

            IRecordsProvider recordsProvider = new InMemoryRecordsProvider(people, friendRelationships, cities);
            IFileStore fileStore = new LocalFileStore();
            var graph = new Graph(recordsProvider, fileStore);


            graph.AddNodeType<Person>(p => p.Id, null, p => p.Age, p => p.AverageIncome, 
                p => p.HighSchoolGraduationDate, p => p.NickName);
            graph.AddNodeType<City>(c => c.Code, null, c => c.Name);

            graph.AddEdgeType<Person, Person>(p => p.FavoritePersonId);
            graph.AddEdgeType<Person, Person, FriendRelationship>(p => p.SourcePersonId, p => p.DestinationPersonId, p => p.DateMet);
            graph.AddEdgeType<Person, City>(p => p.CityCode);

            Assert.IsTrue(graph.NodeTypes.Count == 2);
            Assert.IsTrue(graph.EdgeTypes.Count == 3);

            await graph.SaveEdgesAndNodesToCsv(10, CsvFormat.Neptune);

            Assert.IsTrue(Directory.Exists(graph.GraphId));


        }


        public class City
        {
            [Key]
            public string Code { get; set; }

            public string Name { get; set; }
            }

        public class Person
        {
            [Key]
            public int Id { get; set; }

            public string CityCode { get; set; }

            public string Name { get; set; }

            public string NickName { get; set; }

            public int FavoritePersonId { get; set; }

            public int Age { get; set; }

            public double AverageIncome { get; set; }

            public DateTime HighSchoolGraduationDate { get; set; }
        }


        public class FriendRelationship
        {
            public int SourcePersonId { get; set; }
            public int DestinationPersonId { get; set; }

            public DateTime DateMet { get; set; }
        }
    }
}
