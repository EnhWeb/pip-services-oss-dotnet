using System;
using System.Threading.Tasks;

using PipServices.Commons.Cache;
using Xunit;

namespace PipServices.Oss.Fixtures
{
    public class CacheFixture
    {
        private const string Key1 = "key1";
        private const string Key2 = "key2";
        private const string Key3 = "key3";

        private const string Value1 = "value1";
        private const string Value2 = "value2";
        private const string Value3 = "value3";

        private ICache _cache;

        public CacheFixture(ICache cache)
        {
            _cache = cache;
        }

        public void TestRetrieveBothValueIn500ms()
        {
            Task.Delay(500).Wait();

            var val1 = _cache.RetrieveAsync<string>(null, Key1).Result;
            var val2 = _cache.RetrieveAsync<string>(null, Key2).Result;

            Assert.NotNull(val1);
            Assert.Equal(Value1, val1);

            Assert.NotNull(val2);
            Assert.Equal(Value2, val2);
        }

        public void TestRetrieveBothValueIn1000msFails()
        {
            Task.Delay(1000).Wait();

            var val1 = _cache.RetrieveAsync<string>(null, Key1).Result;
            var val2 = _cache.RetrieveAsync<string>(null, Key2).Result;

            //Assert.Null(val1);
            //Assert.Null(val2);
        }

        public void TestStoreReturnsSameValue()
        {
            var storedVal = _cache.StoreAsync(null, Key3, Value3, 0).Result;
            Assert.Equal(Value3, storedVal);
        }

        public void TestStoreValueIsStored()
        {
            var value = _cache.StoreAsync(null, Key3, Value3, 1000).Result;
            var val3 = _cache.RetrieveAsync<string>(null, Key3).Result;

            Assert.NotNull(val3);
            Assert.Equal(Value3, val3);
        }

        public void TestRemoveValueIsRemoved()
        {
            _cache.RemoveAsync(null, Key1).Wait();

            var val1 = _cache.RetrieveAsync<string>(null, Key1).Result;
            Assert.Null(val1);
        }

        public void TestConfigureNewValueStaysFor1500msButFailsFor2500ms()
        {
            var value = _cache.StoreAsync(null, Key3, Value3, 2000).Result;
            var val3 = _cache.RetrieveAsync<string>(null, Key3).Result;
            Assert.NotNull(val3);
            Assert.Equal(Value3, val3);

            Task.Delay(2500).Wait();

            val3 = _cache.RetrieveAsync<string>(null, Key3).Result;
            Assert.Null(val3);
        }

    }
}
