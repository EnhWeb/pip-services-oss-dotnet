using System;
using System.Collections.Generic;
using PipServices.Commons.Data;
using PipServices.Commons.Refer;
using PipServices.Commons.Config;
using PipServices.Commons.Run;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using PipServices.Data;

namespace PipServices.Oss.Fixtures
{
    public class PersistenceFixture
    {
        private readonly Dummy _dummy1 = new Dummy
        {
            Key = "Key 1",
            Content = "Content 1"
        };

        private readonly Dummy _dummy2 = new Dummy
        {
            Key = "Key 2",
            Content = "Content 2"
        };

        private readonly IReferenceable _refs;
        private readonly IConfigurable _conf;
        private readonly IOpenable _open;
        private readonly IClosable _close;
        private readonly ICleanable _clean;
        private readonly IWriter<Dummy,string> _write;
        private readonly IGetter<Dummy,string> _get;
        private readonly ISetter<Dummy> _set;

        public PersistenceFixture(IReferenceable refs, IConfigurable conf, IOpenable open, IClosable close, ICleanable clean,
            IWriter<Dummy, string> write, IGetter<Dummy, string> get, ISetter<Dummy> set)
        {
            Assert.NotNull(refs);
            _refs = refs;

            Assert.NotNull(conf);
            _conf = conf;

            Assert.NotNull(open);
            _open = open;

            Assert.NotNull(close);
            _close = close;

            Assert.NotNull(clean);
            _clean = clean;

            Assert.NotNull(write);
            _write = write;

            Assert.NotNull(get);
            _get = get;

            Assert.NotNull(set);
            _set = set;
        }

        public async Task TestCrudOperationsAsync()
        {
            // Create one dummy
            var dummy1 = await _write.CreateAsync(null, _dummy1);

            Assert.NotNull(dummy1);
            Assert.NotNull(dummy1.Id);
            Assert.Equal(_dummy1.Key, dummy1.Key);
            Assert.Equal(_dummy1.Content, dummy1.Content);

            // Create another dummy
            var dummy2 = await _write.CreateAsync(null, _dummy2);

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
            var dummy = await _write.UpdateAsync(null, dummy1);

            Assert.NotNull(dummy);
            Assert.Equal(dummy1.Id, dummy.Id);
            Assert.Equal(dummy1.Key, dummy.Key);
            Assert.Equal(dummy1.Content, dummy.Content);

            // Delete the dummy
            await _write.DeleteByIdAsync(null, dummy1.Id);

            // Try to get deleted dummy
            dummy = await _get.GetOneByIdAsync(null, dummy1.Id);
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
                await _write.CreateAsync(null, x);
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
                var dummy = await _write.UpdateAsync(null, x);

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

        private async Task AssertDelete(Dummy dummy)
        {
            await _write.DeleteByIdAsync(null, dummy.Id);

            // Try to get deleted dummy
            var result = await _get.GetOneByIdAsync(null, dummy.Id);
            Assert.Null(result);
        }
    }
}
