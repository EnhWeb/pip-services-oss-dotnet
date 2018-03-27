using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elasticsearch.Net;
using PipServices.Commons.Config;
using PipServices.Commons.Convert;
using PipServices.Commons.Data;
using PipServices.Commons.Errors;
using PipServices.Commons.Log;
using PipServices.Commons.Refer;
using PipServices.Commons.Run;
using PipServices.Container.Info;
using PipServices.Net.Rest;

namespace PipServices.Oss.ElasticSearch
{
    public class ElasticSearchLogger: CachedLogger, IReferenceable, IOpenable
    {
        private FixedRateTimer _timer;
        private HttpConnectionResolver _connectionResolver = new HttpConnectionResolver();
        private ElasticLowLevelClient _client;
        private ContainerInfo _containerInfo;
        private string _index = "log";

        public ElasticSearchLogger()
        { }

		public override void Configure(ConfigParams config)
		{
            base.Configure(config);
            _connectionResolver.Configure(config);

            _index = config.GetAsStringWithDefault("index", _index);
		}

        public virtual void SetReferences(IReferences references)
        {
            _connectionResolver.SetReferences(references);

            _containerInfo = references.GetOneOptional<ContainerInfo>(
                new Descriptor("pip-services", "container-info", "default", "*", "1.0"));
            if (_containerInfo != null && string.IsNullOrEmpty(_source))
                _source = _containerInfo.Name;
        }

		public bool IsOpened()
        {
            return _timer != null;
        }

        public async Task OpenAsync(string correlationId)
        {
            if (IsOpened()) return;

            var connection = await _connectionResolver.ResolveAsync(correlationId);


            var protocol = connection.Protocol;
            var host = connection.Host;
            var port = connection.Port;
            var uri = new Uri($"{protocol}://{host}:{port}");

            // Create client
            var settings = new ConnectionConfiguration(uri)
                .RequestTimeout(TimeSpan.FromMinutes(2))
                .ThrowExceptions(true);
            _client = new ElasticLowLevelClient(settings);

            // Create index if it doesn't exist
            var response = await _client.IndicesExistsAsync<StringResponse>(_index);
            if (response.HttpStatusCode == 404)
            {
                var request = new
                {
                    settings = new
                    {
                        number_of_shards = 1
                    },
                    mappings = new
                    {
                        log_message = new
                        {
                            properties = new
                            {
                                time = new { type = "date", index = true },
                                source = new { type = "keyword", index = true },
                                level = new { type = "keyword", index = true },
                                correlation_id = new { type = "text", index = true },
                                error = new
                                {
                                    type = "object",
                                    properties = new
                                    {
                                        type = new { type = "keyword", index = true },
                                        category = new { type = "keyword", index = true },
                                        status = new { type = "integer", index = false },
                                        code = new { type = "keyword", index = true },
                                        message = new { type = "text", index = false },
                                        details = new { type = "object" },
                                        correlation_id = new { type = "text", index = false },
                                        cause = new { type = "text", index = false },
                                        stack_trace = new { type = "text", index = false }
                                    }
                                },
                                message = new { type = "text", index = false }
                            }
                        }
                    }
                };
                var json = JsonConverter.ToJson(request);
                response = await _client.IndicesCreateAsync<StringResponse>(_index, PostData.String(json));
                if (!response.Success)
                    throw new ConnectionException(correlationId, "CANNOT_CREATE_INDEX", response.Body);
            }
            else if (!response.Success)
            {
                throw new ConnectionException(correlationId, "CONNECTION_FAILED", response.Body);
            }

            if (_timer == null)
            {
                _timer = new FixedRateTimer(OnTimer, _interval, _interval);
                _timer.Start();
            }
        }

        public async Task CloseAsync(string correlationId)
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer = null;
            }

            _client = null;

            await Task.Delay(0);
        }

        private void OnTimer()
        {            
            Dump();
        }

        protected override void Save(List<LogMessage> messages)
		{
            if (_client == null)
                throw new InvalidStateException("elasticsearch_logger", "NOT_OPENED", "ElasticSearchLogger is not opened");

            var bulk = new List<string>();
            foreach (var message in messages)
            {
                bulk.Add(JsonConverter.ToJson(new { index = new { _index = "log", _type = "log_message", _id = IdGenerator.NextLong() } }));
                bulk.Add(JsonConverter.ToJson(message));
            }

            var response = _client.Bulk<StringResponse>(_index, "log_message", PostData.MultiJson(bulk));
            if (!response.Success)
                throw new InvocationException("elasticsearch_logger", "REQUEST_FAILED", response.Body);
		}
	}
}
