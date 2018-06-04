using PipServices.Commons.Log;
using PipServices.Commons.Config;
using PipServices.Commons.Count;
using PipServices.Commons.Refer;
using Xunit;
using PipServices.Oss.Fixtures;
using System;
using PipServices.Commons.Convert;

namespace PipServices.Oss.ElasticSearch
{
    public sealed class ElasticSearchLoggerTest: IDisposable
    {
        private readonly bool _enabled;
        private readonly TestElasticSearchLogger _logger;
        private readonly LoggerFixture _fixture;
        private readonly ElasticSearchLoggerFixture _esFixture;

        public ElasticSearchLoggerTest()
        {
            var ELASTICSEARCH_ENABLED = Environment.GetEnvironmentVariable("ELASTICSEARCH_ENABLED") ?? "true";
            var ELASTICSEARCH_SERVICE_HOST = Environment.GetEnvironmentVariable("ELASTICSEARCH_SERVICE_HOST") ?? "localhost";
            var ELASTICSEARCH_SERVICE_PORT = Environment.GetEnvironmentVariable("ELASTICSEARCH_SERVICE_PORT") ?? "9200";

            _enabled = BooleanConverter.ToBoolean(ELASTICSEARCH_ENABLED);
            if (_enabled)
            {
                _logger = new TestElasticSearchLogger();
                _logger.Configure(ConfigParams.FromTuples(
                    "level", "trace",
                    "source", "test",
                    "index", "log",
                    "daily", true,
                    "connection.host", ELASTICSEARCH_SERVICE_HOST,
                    "connection.port", ELASTICSEARCH_SERVICE_PORT
                ));

                _fixture = new LoggerFixture(_logger);
                _esFixture = new ElasticSearchLoggerFixture(_logger);

                _logger.OpenAsync(null).Wait();
                _logger.OpenAsync(null).Wait();
            }
        }

        public void Dispose()
        {
            if (_logger != null)
            {
                _logger.CloseAsync(null).Wait();
            }
        }

        [Fact]
        public void TestSimpleLogging()
        {
            if (_enabled)
            {
                _fixture.TestSimpleLogging();
                _esFixture.TestSimpleLogging();
            }
        }

        [Fact]
        public void TestErrorLogging()
        {
            if (_enabled)
            {
                _fixture.TestErrorLogging();
                _esFixture.TestErrorLogging();
            }
        }
    }
}
