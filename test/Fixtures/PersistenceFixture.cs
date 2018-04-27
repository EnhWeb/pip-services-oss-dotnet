using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using PipServices.Commons.Data;
using PipServices.Oss.MongoDb;

using MongoDB.Driver;

namespace PipServices.Oss.Fixtures
{
    public class PersistenceFixture
    {
        private readonly Dummy _dummy1 = new Dummy
        {
            Key = "Key 1",
            Content = "Content 1",
            CreateTimeUtc = DateTime.UtcNow,
            InnerDummy = new InnerDummy()
            {
                Description = "Inner Dummy Description"
            },
            DummyType = DummyType.NotDummy
        };

        private readonly Dummy _dummy2 = new Dummy
        {
            Key = "Key 2",
            Content = "Content 2",
            CreateTimeUtc = DateTime.UtcNow,
            DummyType = DummyType.Dummy
        };

        private readonly IdentifiableMongoDbPersistence<Dummy, string> _persistence;

        public PersistenceFixture(IdentifiableMongoDbPersistence<Dummy, string> persistence)
        {
            Assert.NotNull(persistence);

            _persistence = persistence;
        }

        public async Task TestCrudOperationsAsync()
        {
            // Create one dummy
            var dummy1 = await _persistence.CreateAsync(null, _dummy1);

            Assert.NotNull(dummy1);
            Assert.NotNull(dummy1.Id);
            Assert.Equal(_dummy1.Key, dummy1.Key);
            Assert.Equal(_dummy1.Content, dummy1.Content);

            // Create another dummy
            var dummy2 = await _persistence.CreateAsync(null, _dummy2);

            Assert.NotNull(dummy2);
            Assert.NotNull(dummy2.Id);
            Assert.Equal(_dummy2.Key, dummy2.Key);
            Assert.Equal(_dummy2.Content, dummy2.Content);

            //// Get all dummies
            //var dummies = await _get.GetAllAsync(null);
            //Assert.NotNull(dummies);
            //Assert.Equal(2, dummies.Count());

            // Update the dummy
            dummy1.Content = "Updated Content 1";
            var dummy = await _persistence.UpdateAsync(null, dummy1);

            Assert.NotNull(dummy);
            Assert.Equal(dummy1.Id, dummy.Id);
            Assert.Equal(dummy1.Key, dummy.Key);
            Assert.Equal(dummy1.Content, dummy.Content);

            // Delete the dummy
            await _persistence.DeleteByIdAsync(null, dummy1.Id);

            // Try to get deleted dummy
            dummy = await _persistence.GetOneByIdAsync(null, dummy1.Id);
            Assert.Null(dummy);
        }

        public async Task TestMultithreading()
        {
            const int itemNumber = 50;

            var dummies = new List<Dummy>();

            for (var i = 0; i < itemNumber; i++)
            {
                dummies.Add(new Dummy() {Id = i.ToString(), Key = "Key " + i, Content = "Content " + i});
            }

            var count = 0;
            dummies.AsParallel().ForAll(async x =>
            {
                await _persistence.CreateAsync(null, x);
                Interlocked.Increment(ref count);
            });

            while (count < itemNumber)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(10));
            }

            //var dummiesResponce = await _get.GetAllAsync(null);
            //Assert.NotNull(dummies);
            //Assert.Equal(itemNumber, dummiesResponce.Count());
            //Assert.Equal(itemNumber, dummiesResponce.Total);

            dummies.AsParallel().ForAll(async x =>
            {
                var updatedContent = "Updated Content " + x.Id;

                // Update the dummy
                x.Content = updatedContent;
                var dummy = await _persistence.UpdateAsync(null, x);

                Assert.NotNull(dummy);
                Assert.Equal(x.Id, dummy.Id);
                Assert.Equal(x.Key, dummy.Key);
                Assert.Equal(updatedContent, dummy.Content);
            });

            var taskList = new List<Task>();
            foreach (var dummy in dummies)
            {
                taskList.Add(AssertDelete(dummy));
            }

            Task.WaitAll(taskList.ToArray(), CancellationToken.None);

            //count = 0;
            //dummies.AsParallel().ForAll(async x =>
            //{
            //    // Delete the dummy
            //    await _write.DeleteByIdAsync(null, x.Id);

            //    // Try to get deleted dummy
            //    var dummy = await _get.GetOneByIdAsync(null, x.Id);
            //    Assert.Null(dummy);

            //    Interlocked.Increment(ref count);
            //});

            //while (count < itemNumber)
            //{
            //    await Task.Delay(TimeSpan.FromMilliseconds(10));
            //}

            //dummiesResponce = await _get.GetAllAsync(null);
            //Assert.NotNull(dummies);
            //Assert.Equal(0, dummiesResponce.Count());
            //Assert.Equal(0, dummiesResponce.Total);
        }

        public async Task TestGetByWrongIdAndProjection()
        {
            // arrange
            var dummy = await _persistence.CreateAsync(null, _dummy1);
            var projection = ProjectionParams.FromValues("InnerDummy.Description", "Content", "Key");

            // act
            dynamic result = await _persistence.GetOneByIdAsync(null, "wrong_id", projection);

            // assert
            Assert.Null(result);
        }

        public async Task TestGetByIdAndProjection()
        {
            // arrange
            var dummy = await _persistence.CreateAsync(null, _dummy1);
            var projection = ProjectionParams.FromValues("InnerDummy.Description", "Content", "Key", "CreateTimeUtc", "DummyType");

            // act
            dynamic result = await _persistence.GetOneByIdAsync(null, dummy.Id, projection);

            // assert
            Assert.NotNull(result);
            Assert.Equal(dummy.Key, result.Key);
            Assert.Equal(dummy.Content, result.Content);
            Assert.Equal(dummy.InnerDummy.Description, result.InnerDummy.Description);
            Assert.Equal(dummy.CreateTimeUtc.ToString(), result.CreateTimeUtc.ToString());
            Assert.Equal(dummy.DummyType.ToString(), result.DummyType.ToString());
        }

        public async Task TestGetByIdAndWrongProjection()
        {
            // arrange
            var dummy = await _persistence.CreateAsync(null, _dummy1);
            var projection = ProjectionParams.FromValues("Wrong_Key", "Wrong_Content");

            // act
            dynamic result = await _persistence.GetOneByIdAsync(null, dummy.Id, projection);

            // assert
            Assert.Null(result);
        }

        public async Task TestGetByIdAndNullProjection()
        {
            // arrange
            var dummy = await _persistence.CreateAsync(null, _dummy1);

            // act
            dynamic result = await _persistence.GetOneByIdAsync(null, dummy.Id, null);

            // assert
            Assert.NotNull(result);
            Assert.Equal(dummy.Key, result.Key);
            Assert.Equal(dummy.Content, result.Content);
            Assert.Equal(dummy.InnerDummy.Description, result.InnerDummy.Description);
        }

        public async Task TestGetPageByFilter()
        {
            // arrange 
            var dummy1 = await _persistence.CreateAsync(null, _dummy1);
            var dummy2 = await _persistence.CreateAsync(null, _dummy2);

            var builder = Builders<Dummy>.Filter;
            var filter = builder.Empty;

            // act
            var result = await _persistence.GetPageByFilterAsync(null, filter);

            // assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Data.Count);
        }

        public async Task TestGetPageByProjection()
        {
            // arrange 
            var dummy1 = await _persistence.CreateAsync(null, _dummy1);
            var dummy2 = await _persistence.CreateAsync(null, _dummy2);

            var builder = Builders<Dummy>.Filter;
            var filter = builder.Empty;

            var projection = ProjectionParams.FromValues("InnerDummy.Description", "Content", "Key", "CreateTimeUtc");

            // act
            dynamic result = await _persistence.GetPageByFilterAndProjectionAsync(null, filter, null, null, projection);

            // assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Data.Count);
            Assert.Equal(dummy1.Key, result.Data[0].Key);
            Assert.Equal(dummy1.Content, result.Data[0].Content);
            Assert.Equal(dummy1.InnerDummy.Description, result.Data[0].InnerDummy.Description);
            Assert.Equal(dummy1.CreateTimeUtc.ToString(), result.Data[0].CreateTimeUtc.ToString());
            Assert.Equal(dummy2.Key, result.Data[1].Key);
            Assert.Equal(dummy2.Content, result.Data[1].Content);
        }

        public async Task TestGetPageByNullProjection()
        {
            // arrange 
            var dummy1 = await _persistence.CreateAsync(null, _dummy1);
            var dummy2 = await _persistence.CreateAsync(null, _dummy2);

            var builder = Builders<Dummy>.Filter;
            var filter = builder.Empty;

            // act
            var result = await _persistence.GetPageByFilterAndProjectionAsync(null, filter);

            // assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Data.Count);
        }

        public async Task TestGetPageByWrongProjection()
        {
            // arrange 
            var dummy1 = await _persistence.CreateAsync(null, _dummy1);
            var dummy2 = await _persistence.CreateAsync(null, _dummy2);

            var builder = Builders<Dummy>.Filter;
            var filter = builder.Empty;

            var projection = ProjectionParams.FromValues("Wrong_InnerDummy.Description", "Wrong_Content", "Wrong_Key");

            // act
            dynamic result = await _persistence.GetPageByFilterAndProjectionAsync(null, filter, null, null, projection);

            // assert
            Assert.NotNull(result);
            Assert.Empty(result.Data);
        }

        private async Task AssertDelete(Dummy dummy)
        {
            await _persistence.DeleteByIdAsync(null, dummy.Id);

            // Try to get deleted dummy
            var result = await _persistence.GetOneByIdAsync(null, dummy.Id);
            Assert.Null(result);
        }
    }
}
