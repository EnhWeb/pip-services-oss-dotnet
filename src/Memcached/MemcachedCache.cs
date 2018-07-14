using System;

using PipServices.Commons.Cache;
using PipServices.Commons.Config;
using PipServices.Commons.Refer;
using PipServices.Commons.Run;
using PipServices.Commons.Auth;
using PipServices.Commons.Connect;
using PipServices.Commons.Errors;
using PipServices.Commons.Convert;

using System.Threading.Tasks;
using Enyim.Caching;
using Enyim.Caching.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PipServices.Oss.Memcached
{
    public class MemcachedCache : ICache, IConfigurable, IReferenceable, IOpenable
    {
        private ConnectionResolver _connectionResolver = new ConnectionResolver();
        private CredentialResolver _credentialResolver = new CredentialResolver();
        private MemcachedClient _client = null;

        public MemcachedCache()
        {
        }

        public void Configure(ConfigParams config)
        {
            _connectionResolver.Configure(config);
            _credentialResolver.Configure(config);
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
            var connections = await _connectionResolver.ResolveAllAsync(correlationId);
            if (connections.Count == 0)
                throw new ConfigException(correlationId, "NO_CONNECTION", "Connection is not configured");

            var options = new MemcachedClientConfiguration();

            foreach (var connection in connections)
            {
                var uri = connection.Uri;

                if (!string.IsNullOrEmpty(uri))
                {
                    options.AddServer(uri, 11211);
                }
                else 
                {
                    var host = connection.Host ?? "localhost";
                    var port = connection.Port != 0 ? connection.Port : 11211;

                    options.AddServer(host, port);
                }
            }

            _client = new MemcachedClient(null, options);
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

        public async Task<object> RetrieveAsync(string correlationId, string key)
        {
            CheckOpened(correlationId);

            return await _client.GetAsync<object>(key);
        }

        public async Task<object> StoreAsync(string correlationId, string key, object value, long timeout)
        {
            CheckOpened(correlationId);

            await _client.StoreAsync(Enyim.Caching.Memcached.StoreMode.Set, key, value, TimeSpan.FromMilliseconds(timeout));

            return value;
        }

        public async Task RemoveAsync(string correlationId, string key)
        {
            CheckOpened(correlationId);

            await _client.RemoveAsync(key);
        }
    }
}
