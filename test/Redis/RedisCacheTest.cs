using System;

using PipServices.Commons.Config;
using PipServices.Commons.Convert;
using PipServices.Oss.Fixtures;

using Xunit;

namespace PipServices.Oss.Redis
{
    [Collection("Sequential")]
    public class RedisCacheTest: IDisposable
    {
        private readonly bool _enabled;
        private readonly RedisCache _cache;
        private readonly CacheFixture _fixture;

        public RedisCacheTest()
        {
            var REDIS_ENABLED = Environment.GetEnvironmentVariable("REDIS_ENABLED") ?? "true";
            var REDIS_SERVICE_HOST = Environment.GetEnvironmentVariable("REDIS_SERVICE_HOST") ?? "localhost";
            var REDIS_SERVICE_PORT = Environment.GetEnvironmentVariable("REDIS_SERVICE_PORT") ?? "6379";

            _enabled = BooleanConverter.ToBoolean(REDIS_ENABLED);
            if (_enabled)
            {
                _cache = new RedisCache();
                _cache.Configure(ConfigParams.FromTuples(
                    "connection.host", REDIS_SERVICE_HOST,
                    "connection.port", REDIS_SERVICE_PORT
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
