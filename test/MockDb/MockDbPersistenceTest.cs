using System;

using Xunit;

using PipServices.Oss.Fixtures;

namespace PipServices.Oss.MockDb
{

    public sealed class MockDbPersistenceTest
    {
        private static PersistenceFixture Fixture { get; set; }

        public MockDbPersistenceTest()
        {
            Fixture = new PersistenceFixture(new MockDummyPersistence());
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
        public void It_Should_Get_By_Id_And_Id_Projection()
        {
            Fixture?.TestGetByIdAndIdProjection().Wait();
        }
    }
}
