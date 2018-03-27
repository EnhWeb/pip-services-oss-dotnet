using System;
using PipServices.Commons.Config;
using PipServices.Commons.Convert;
using PipServices.Commons.Refer;
using PipServices.Container.Info;
using PipServices.Oss.Fixtures;
using Xunit;

namespace PipServices.Oss.Prometheus
{
    public sealed class PrometheusCountersTest : IDisposable
    {
        private readonly bool _enabled;
        private readonly PrometheusCounters _counters;
        private readonly CountersFixture _fixture;

        public PrometheusCountersTest()
        {
            var PUSHGATEWAY_ENABLED = Environment.GetEnvironmentVariable("PUSHGATEWAY_ENABLED") ?? "true";
            var PUSHGATEWAY_SERVICE_HOST = Environment.GetEnvironmentVariable("PUSHGATEWAY_SERVICE_HOST") ?? "localhost";
            var PUSHGATEWAY_SERVICE_PORT = Environment.GetEnvironmentVariable("PUSHGATEWAY_SERVICE_PORT") ?? "9091";

            _enabled = BooleanConverter.ToBoolean(PUSHGATEWAY_ENABLED);
            if (_enabled)
            {
                _counters = new PrometheusCounters();
                _counters.Configure(ConfigParams.FromTuples(
                    "interval", "5000",
                    "connection.host", PUSHGATEWAY_SERVICE_HOST,
                    "connection.port", PUSHGATEWAY_SERVICE_PORT
                ));

                var containerInfo = new ContainerInfo();
                containerInfo.Name = "Test";
                containerInfo.Description = "This is a test container";
                containerInfo.ContainerId = "Test";

                var references = References.FromTuples(
                    new Descriptor("pip-services", "container-info", "default", "default", "1.0"), containerInfo,
                    new Descriptor("pip-services", "counters", "prometheus", "default", "1.0"), _counters
                );
                _counters.SetReferences(references);

                _fixture = new CountersFixture(_counters);

                _counters.OpenAsync(null).Wait();
            }
        }

        public void Dispose()
        {
            if (_counters != null)
                _counters.CloseAsync(null).Wait();
        }

        [Fact]
        public void TestSimpleCounters()
        {
            if (_enabled)
                _fixture.TestSimpleCounters();
        }

        [Fact]
        public void TestMeasureElapsedTime()
        {
            if (_enabled)
                _fixture.TestMeasureElapsedTime();
        }
    }
}
