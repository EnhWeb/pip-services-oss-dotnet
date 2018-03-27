using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PipServices.Commons.Refer;
using PipServices.Net.Rest;

namespace PipServices.Oss.Prometheus
{
    public class PrometheusMetricsService: RestService
    {
        private PrometheusCounters _prometheusCounters;

        public PrometheusMetricsService()
        {
            _dependencyResolver.Put("counters", new Descriptor("pip-services", "counters", "prometheus", "*", "1.0"));
        }

        public override void SetReferences(IReferences references)
        {
            base.SetReferences(references);

            _prometheusCounters = _dependencyResolver.GetOneOptional<PrometheusCounters>("counters");
        }

        public override void Register()
        {
            RegisterRoute("get", "metrics", Metrics);
        }

        private async Task Metrics(HttpRequest request, HttpResponse response, RouteData routeData)
        {
            var counters = _prometheusCounters != null ? _prometheusCounters.GetAll() : null;
            var body = PrometheusCounterConverter.ToString(counters);

            response.StatusCode = 200;
            response.ContentType = "text/plain";
            await response.WriteAsync(body);
        }
    }
}
