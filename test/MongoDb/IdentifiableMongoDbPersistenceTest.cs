using System;
using System.Threading;
using PipServices.Commons.Config;
using Xunit;
using PipServices.Oss.Fixtures;

namespace PipServices.Oss.MongoDb
{
    public sealed class IdentifiableMongoDbPersistenceTest : IDisposable
    {
        private static IdentifiableMongoDbPersistence<Dummy, string> Db { get; } 
            = new IdentifiableMongoDbPersistence<Dummy, string>("dummies");
        private static PersistenceFixture Fixture { get; set; }

        public IdentifiableMongoDbPersistenceTest()
        {
            var mongoUri = Environment.GetEnvironmentVariable("MONGO_URI") ?? "mongodb://localhost:27017/test";
            var mongoHost = Environment.GetEnvironmentVariable("MONGO_HOST") ?? "localhost";
            var mongoPort = Environment.GetEnvironmentVariable("MONGO_PORT") ?? "27017";
            var mongoDatabase = Environment.GetEnvironmentVariable("MONGO_DB") ?? "test";

            if (mongoUri == null && mongoHost == null)
                return;

            if (Db == null) return;

            Db.Configure(ConfigParams.FromTuples(
                "connection.uri", mongoUri,
                "connection.host", mongoHost,
                "connection.port", mongoPort,
                "connection.database", mongoDatabase
            ));

            Db.OpenAsync(null).Wait();
            Db.ClearAsync(null).Wait();

            Fixture = new PersistenceFixture(Db);
        }

        [Fact]
        public void TestCrudOperations()
        {
            Fixture?.TestCrudOperationsAsync().Wait();
        }

        [Fact]
        public void TestMultithreading()
        {
            Fixture?.TestMultithreading().Wait();
        }

        [Fact]
        public void It_Should_Not_Get_By_Wrong_Id_And_Projection()
        {
            Fixture?.TestGetByWrongIdAndProjection().Wait();
        }

        [Fact]
        public void It_Should_Get_By_Id_And_Projection()
        {
            Fixture?.TestGetByIdAndProjection().Wait();
        }

        [Fact]
        public void It_Should_Get_By_Id_And_Wrong_Projection()
        {
            Fixture?.TestGetByIdAndWrongProjection().Wait();
        }

        [Fact]
        public void It_Should_Get_By_Id_And_Null_Projection()
        {
            Fixture?.TestGetByIdAndNullProjection().Wait();
        }

        [Fact]
        public void It_Should_Get_Page_By_Filter()
        {
            Fixture?.TestGetPageByFilter().Wait();
        }

        [Fact]
        public void It_Should_Get_Page_By_Projection()
        {
            Fixture?.TestGetPageByProjection().Wait();
        }

        [Fact]
        public void It_Should_Get_Page_By_Null_Projection()
        {
            Fixture?.TestGetPageByNullProjection().Wait();
        }

        [Fact]
        public void It_Should_Not_Get_Page_By_Wrong_Projection()
        {
            Fixture?.TestGetPageByWrongProjection().Wait();
        }

        [Fact]
        public void It_Should_Modify_Object_With_Existing_Properties_By_Selected_Fields()
        {
            Fixture?.TestModifyExistingPropertiesBySelectedFields().Wait();
        }

        [Fact]
        public void It_Should_Modify_Object_With_Null_Properties_By_Selected_Fields()
        {
            Fixture?.TestModifyExistingPropertiesBySelectedFields().Wait();
        }

        public void Dispose()
        {
            Db?.CloseAsync(null).Wait();
        }
    }
}
