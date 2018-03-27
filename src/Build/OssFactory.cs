using System;
using PipServices.Commons.Build;
using PipServices.Commons.Refer;
using PipServices.Oss.ElasticSearch;
using PipServices.Oss.Prometheus;

namespace PipServices.Oss.Build
{
    public class OssFactory: Factory
    {
        public static readonly Descriptor Descriptor = new Descriptor("pip-services", "factory", "logger", "default", "1.0");
        public static readonly Descriptor ElasticSearchLoggerDescriptor = new Descriptor("pip-services", "logger", "elasticsearch", "*", "1.0");
        public static readonly Descriptor PrometheusCountersDescriptor = new Descriptor("pip-services", "counters", "prometheus", "*", "1.0");
        public static readonly Descriptor PrometheusMetricsServiceDescriptor = new Descriptor("pip-services", "metrics-service", "prometheus", "*", "1.0");

        public OssFactory()
        {
            RegisterAsType(OssFactory.ElasticSearchLoggerDescriptor, typeof(ElasticSearchLogger));
            RegisterAsType(OssFactory.PrometheusCountersDescriptor, typeof(PrometheusCounters));
            RegisterAsType(OssFactory.PrometheusMetricsServiceDescriptor, typeof(PrometheusMetricsService));
        }
    }
}
