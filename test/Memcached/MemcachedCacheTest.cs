using System;
using System.Threading.Tasks;
using PipServices.Commons.Config;
using PipServices.Commons.Convert;
using PipServices.Oss.Fixtures;
using Xunit;

namespace PipServices.Oss.Memcached
{
    [Collection("Sequential")]
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

        [Fact]
        public void It_Should_Store_Cached_Value()
        {
            _fixture.TestStoreAsync();
        }

        [Fact]
        public void It_Should_Store_And_Retrieve_Cached_Value()
        {
            _fixture.TestStoreAndRetrieveAsync();
        }

        [Fact]
        public void It_Should_Store_And_Retrieve_Expired_Cached_Value()
        {
            _fixture.TestStoreAndRetrieveExpiredAsync();
        }

        [Fact]
        public void It_Should_Store_And_Retrieve_Removed_Cached_Value3()
        {
            _fixture.TestStoreAndRetrieveRemovedAsync();
        }
    }
}
