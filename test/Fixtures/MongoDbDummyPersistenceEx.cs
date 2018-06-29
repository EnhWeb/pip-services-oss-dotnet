﻿
using System.Threading.Tasks;

using PipServices.Commons.Data;
using PipServices.Oss.MongoDb;

namespace PipServices.Oss.Fixtures
{
    public class MongoDbDummyPersistenceEx : IdentifiableMongoDbPersistenceEx<Dummy, string>, IDummyPersistence
    {
        public MongoDbDummyPersistenceEx()
            : base("dummies2")
        {
        }

        public async Task ClearAsync()
        {
            await ClearAsync(null);
        }

        public async Task<Dummy> DeleteAsync(string correlationId, string id)
        {
            return await DeleteByIdAsync(correlationId, id);
        }

        public async Task<DataPage<Dummy>> GetAsync(string correlationId, FilterParams filter, PagingParams paging)
        {
            return await GetPageByFilterAsync(correlationId, ComposeFilter(filter), paging);
        }

        public async Task<DataPage<object>> GetAsync(string correlationId, FilterParams filter, PagingParams paging, ProjectionParams projection)
        {
            return await GetPageByFilterAndProjectionAsync(correlationId, ComposeFilter(filter), paging, null, projection);
        }

        public async Task<Dummy> GetByIdAsync(string correlationId, string id)
        {
            return await GetOneByIdAsync(correlationId, id);
        }

        public async Task<object> GetByIdAsync(string correlationId, string id, ProjectionParams projection)
        {
            return await GetOneByIdAsync(correlationId, id, projection);
        }

        public async Task<Dummy> ModifyAsync(string correlationId, string id, AnyValueMap updateMap)
        {
            return await ModifyByIdAsync(correlationId, id, ComposeUpdate(updateMap));
        }
    }
}