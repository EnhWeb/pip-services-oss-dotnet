using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PipServices.Commons.Refer;
using PipServices.Commons.Count;
using PipServices.Net.Rest;

namespace PipServices.Oss.Prometheus
{
    public class PrometheusMetricsService: RestService
    {
        private CachedCounters _cachedCounters;

        public PrometheusMetricsService()
        {
            _dependencyResolver.Put("cached-counters", new Descriptor("pip-services", "counters", "cached", "*", "1.0"));
            _dependencyResolver.Put("prometheus-counters", new Descriptor("pip-services", "counters", "prometheus", "*", "1.0"));
        }

        public override void SetReferences(IReferences references)
        {
            base.SetReferences(references);

            _cachedCounters = _dependencyResolver.GetOneOptional<PrometheusCounters>("prometheus-counters");
            if (_cachedCounters == null)
                _cachedCounters = _dependencyResolver.GetOneOptional<CachedCounters>("cached-counters");
        }

        public override void Register()
        {
            RegisterRoute("get", "metrics", Metrics);
        }

        private async Task Metrics(HttpRequest request, HttpResponse response, RouteData routeData)
        {
            var counters = _cachedCounters != null ? _cachedCounters.GetAll() : null;
            var body = PrometheusCounterConverter.ToString(counters);

            response.StatusCode = 200;
            response.ContentType = "text/plain";
            await response.WriteAsync(body);
        }
    }
}
