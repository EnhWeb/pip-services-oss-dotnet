using System;
using PipServices.Commons.Config;
using PipServices.Commons.Refer;
using PipServices.Commons.Run;
using PipServices.Commons.Auth;
using PipServices.Commons.Connect;
using PipServices.Commons.Errors;

using System.Threading.Tasks;
using Enyim.Caching;
using Enyim.Caching.Configuration;
using PipServices.Commons.Lock;

namespace PipServices.Oss.Memcached
{
    public class MemcachedLock : Lock, IConfigurable, IReferenceable, IOpenable
    {
        private ConnectionResolver _connectionResolver = new ConnectionResolver();
        private CredentialResolver _credentialResolver = new CredentialResolver();
        private MemcachedClient _client = null;

        public MemcachedLock()
        {
        }

        // Todo: Make the method virtual
        public new void Configure(ConfigParams config)
        {
            base.Configure(config);
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

        public override bool TryAcquireLock(string correlationId, string key, long ttl)
        {
            CheckOpened(correlationId);

            return _client.StoreAsync(Enyim.Caching.Memcached.StoreMode.Add, key, "lock", TimeSpan.FromMilliseconds(ttl)).Result;
        }

        public override void ReleaseLock(string correlationId, string key)
        {
            CheckOpened(correlationId);

            _client.RemoveAsync(key).Wait();
        }
    }
}
