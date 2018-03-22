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

            Fixture = new PersistenceFixture(Db, Db, Db, Db, Db, Db, Db, Db);
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

        public void Dispose()
        {
            Db?.CloseAsync(null).Wait();
        }
    }
}
