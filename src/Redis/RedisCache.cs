using System;

using PipServices.Commons.Cache;
using PipServices.Commons.Config;
using PipServices.Commons.Refer;
using PipServices.Commons.Run;
using PipServices.Commons.Auth;
using PipServices.Commons.Connect;
using PipServices.Commons.Errors;
using PipServices.Commons.Convert;

using ServiceStack.Redis;
using System.Threading.Tasks;

namespace PipServices.Oss.Redis
{
    public class RedisCache: ICache, IConfigurable, IReferenceable, IOpenable
    {
        private ConnectionResolver _connectionResolver = new ConnectionResolver();
        private CredentialResolver _credentialResolver = new CredentialResolver();

        private int _connectTimeout = 30000;
        private int _retryTimeout = 3000;
        private int _retries = 3;
        private IRedisClient _client = null;

        public RedisCache()
        {
        }

        public void Configure(ConfigParams config)
        {
            _connectionResolver.Configure(config);
            _credentialResolver.Configure(config);

            _connectTimeout = config.GetAsIntegerWithDefault("options.connect_timeout", _connectTimeout);
            _retryTimeout = config.GetAsIntegerWithDefault("options.timeout", _retryTimeout);
            _retries = config.GetAsIntegerWithDefault("options.retries", _retries);
        }

        public void SetReferences(IReferences references)
        {
            _connectionResolver.SetReferences(references);
            _credentialResolver.SetReferences(references);
        }

        public bool IsOpened()
        {
            return _client != null;
        }

        public async Task OpenAsync(string correlationId)
        {
            var connection = await _connectionResolver.ResolveAsync(correlationId);
            if (connection == null)
                throw new ConfigException(correlationId, "NO_CONNECTION", "Connection is not configured");

            var uri = connection.Uri;
            if (!string.IsNullOrEmpty(uri))
            {
                _client = new RedisClient(new Uri(uri));
            }
            else
            {                
                var host = connection.Host ?? "localhost";
                var port = connection.Port != 0 ? connection.Port : 6379;
                _client = new RedisClient(host, port);
            }

            var credential = await _credentialResolver.LookupAsync(correlationId);
            if (credential != null && !string.IsNullOrEmpty(credential.Password))
            {
                _client.Password = credential.Password;
            }
 
            _client.ConnectTimeout = _connectTimeout;
            _client.RetryTimeout = _retryTimeout;
            _client.RetryCount = _retries;
        }

        public async Task CloseAsync(string correlationId)
        {
            if (_client != null)
            {
                _client.Dispose();
                _client = null;
            }

            await Task.Delay(0);
        }

        private void CheckOpened(string correlationId)
        {
            if (!IsOpened())
                throw new InvalidStateException(correlationId, "NOT_OPENED", "Connection is not opened");
        }

        public async Task<T> RetrieveAsync<T>(string correlationId, string key)
        {
            CheckOpened(correlationId);

            var json = _client.GetValue(key);
            var value = JsonConverter.FromJson<T>(json);

            return await Task.FromResult(value);
        }

        public async Task<T> StoreAsync<T>(string correlationId, string key, T value, long timeout)
        {
            CheckOpened(correlationId);

            var json = JsonConverter.ToJson(value);
            _client.SetValue(key, json, TimeSpan.FromMilliseconds(timeout));

            return await Task.FromResult(value);
        }

        public async Task RemoveAsync(string correlationId, string key)
        {
            CheckOpened(correlationId);

            _client.Remove(key);

            await Task.Delay(0);
        }
    }
}
