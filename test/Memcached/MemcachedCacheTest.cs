using System;
using System.Threading.Tasks;
using PipServices.Commons.Config;
using PipServices.Commons.Convert;
using PipServices.Oss.Fixtures;
using Xunit;

namespace PipServices.Oss.Memcached
{
    public class MemcachedCacheTest : IDisposable
    {
        private readonly bool _enabled;
        private readonly MemcachedCache _cache;
        private readonly CacheFixture _fixture;

        public MemcachedCacheTest()
        {
            var MEMCACHED_ENABLED = Environment.GetEnvironmentVariable("MEMCACHED_ENABLED") ?? "true";
            var MEMCACHED_SERVICE_HOST = Environment.GetEnvironmentVariable("MEMCACHED_SERVICE_HOST") ?? "localhost";
            var MEMCACHED_SERVICE_PORT = Environment.GetEnvironmentVariable("MEMCACHED_SERVICE_PORT") ?? "11211";

            _enabled = BooleanConverter.ToBoolean(MEMCACHED_ENABLED);
            if (_enabled)
            {
                _cache = new MemcachedCache();
                _cache.Configure(ConfigParams.FromTuples(
                    "connection.host", MEMCACHED_SERVICE_HOST,
                    "connection.port", MEMCACHED_SERVICE_PORT
                ));

                _fixture = new CacheFixture(_cache);

                _cache.OpenAsync(null).Wait();
            }
        }

        public void Dispose()
        {
            if (_cache != null)
            {
                _cache.CloseAsync(null).Wait();
            }
        }

        //[Fact]
        //public void Retrieve_BothValue_In500ms()
        //{
        //    _fixture.TestRetrieveBothValueIn500ms();
        //}

        [Fact]
        public void Retrieve_BothValue_In1000ms_Fails()
        {
            _fixture.TestRetrieveBothValueIn1000msFails();
        }

        [Fact]
        public void Store_ReturnsSameValue()
        {
            _fixture.TestStoreReturnsSameValue();
        }

        //[Fact]
        //public void Store_ValueIsStored()
        //{
        //    _fixture.TestStoreValueIsStored();
        //}

        [Fact]
        public void Remove_ValueIsRemoved()
        {
            _fixture.TestRemoveValueIsRemoved();
        }

        //[Fact]
        //public void Configure_NewValueStaysFor1500ms_ButFailsFor2500ms()
        //{
        //    _fixture.TestConfigureNewValueStaysFor1500msButFailsFor2500ms();
        //}
    }
}
