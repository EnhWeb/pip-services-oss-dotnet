using System;
using System.Threading.Tasks;
using PipServices.Commons.Config;
using PipServices.Commons.Convert;
using PipServices.Commons.Refer;
using PipServices.Container.Info;
using Xunit;

namespace PipServices.Oss.Prometheus
{
    public class PrometheusMetricsRestServiceTest : IDisposable
    {
        private PrometheusMetricsRestService _service;
        private PrometheusCounters _counters;

        public PrometheusMetricsRestServiceTest()
        {
            var config = ConfigParams.FromTuples(
                "connection.protocol", "http",
                "connection.host", "localhost",
                "connection.port", "3000"
            );
            _service = new PrometheusMetricsRestService();
            _service.Configure(config);

            var containerInfo = new ContainerInfo();
            containerInfo.Name = "Test";
            containerInfo.Description = "This is a test container";

            _counters = new PrometheusCounters();

            var references = References.FromTuples(
                new Descriptor("pip-services", "container-info", "default", "default", "1.0"), containerInfo,
                new Descriptor("pip-services", "counters", "prometheus", "default", "1.0"), _counters,
                new Descriptor("pip-services", "metrics-service", "prometheus", "default", "1.0"), _service
            );
            _service.SetReferences(references);


            _counters.OpenAsync(null).Wait();
            _service.OpenAsync(null).Wait();

            Task.Delay(500).Wait();
        }

        public void Dispose()
        {
            _service.CloseAsync(null).Wait();
            _counters.CloseAsync(null).Wait();
        }

        [Fact]
        public async Task TestMetricsAsync()
        {
            _counters.IncrementOne("test.counter1");
            _counters.Stats("test.counter2", 2);
            _counters.Last("test.counter3", 3);
            _counters.TimestampNow("test.counter4");

            String status = await Invoke("/metrics");
            Assert.NotNull(status);
            Assert.True(status.Length > 0);
        }

        private static async Task<string> Invoke(string route)
        {
            using (var httpClient = new System.Net.Http.HttpClient())
            {
                var response = await httpClient.GetAsync("http://localhost:3000" + route);
                var responseValue = response.Content.ReadAsStringAsync().Result;
                return await Task.FromResult(responseValue);
            }
        }
    }
}
