using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using PipServices.Commons.Config;
using PipServices.Commons.Count;
using PipServices.Commons.Info;
using PipServices.Commons.Log;
using PipServices.Commons.Refer;
using PipServices.Commons.Run;
using PipServices.Net.Rest;

namespace PipServices.Oss.Prometheus
{
    public class PrometheusCounters: CachedCounters, IReferenceable, IOpenable
    {
        private CompositeLogger _logger = new CompositeLogger();
        private HttpConnectionResolver _connectionResolver = new HttpConnectionResolver();
        private bool _opened;
        private string _source;
        private string _instance;
        private HttpClient _client;
        private Uri _requestUri;

        public PrometheusCounters()
        { }

        public override void Configure(ConfigParams config)
        {
            base.Configure(config);

            _connectionResolver.Configure(config);
            _source = config.GetAsStringWithDefault("source", _source);
            _instance = config.GetAsStringWithDefault("instance", _instance);
        }

        public virtual void SetReferences(IReferences references)
        {
            _logger.SetReferences(references);
            _connectionResolver.SetReferences(references);

            var contextInfo = references.GetOneOptional<ContextInfo>(
                new Descriptor("pip-services", "context-info", "default", "*", "1.0"));
            if (contextInfo != null && string.IsNullOrEmpty(_source))
                _source = contextInfo.Name;
            if (contextInfo != null && string.IsNullOrEmpty(_instance))
                _instance = contextInfo.ContextId;
        }

        public bool IsOpened()
        {
            return _opened;
        }

        public async Task OpenAsync(string correlationId)
        {
            if (_opened) return;

            try
            {
                var connection = await _connectionResolver.ResolveAsync(correlationId);
                var job = _source ?? "unknown";
                var instance = _instance ?? Environment.MachineName;
                var route = $"{connection.Uri}/metrics/job/{job}/instance/{instance}";
                _requestUri = new Uri(route, UriKind.Absolute);

                _client = new HttpClient();
            }
            catch (Exception ex)
            {
                _client = null;
                _logger.Warn(correlationId, "Connection to Prometheus server is not configured: " + ex.Message);
            }
            finally
            {
                _opened = true;
            }
        }

        public async Task CloseAsync(string correlationId)
        {
            _opened = false;
            _client = null;
            _requestUri = null;

            await Task.Delay(0);
        }

		protected override void Save(IEnumerable<Counter> counters)
		{
            if (_client == null) return;

            try
            {
                var body = PrometheusCounterConverter.ToString(counters, null, null);

                using (HttpContent requestContent = new StringContent(body, Encoding.UTF8, "text/plain"))
                {
                    HttpResponseMessage response = _client.PutAsync(_requestUri, requestContent).Result;
                    if ((int)response.StatusCode >= 400)
                        _logger.Error("prometheus-counters", "Failed to push metrics to prometheus");
                }
            }
            catch (Exception ex)
            {
                _logger.Error("prometheus-counters", ex, "Failed to push metrics to prometheus");
            }
		}
	}
}
