﻿
using System.Threading.Tasks;

using PipServices.Commons.Data;
using PipServices.Oss.MongoDb;

namespace PipServices.Oss.Fixtures
{
    public class MongoDbDummyPersistence : IdentifiableMongoDbPersistence<Dummy, string>, IDummyPersistence
    {
        public MongoDbDummyPersistence()
            : base("dummies")
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

        public async Task<DataPage<Dummy>> GetAsync(string correlationId, FilterParams filter, PagingParams paging, SortParams sort)
        {
            return await GetPageByFilterAsync(correlationId, ComposeFilter(filter), paging, ComposeSort(sort));
        }

        public async Task<DataPage<object>> GetAsync(string correlationId, FilterParams filter, PagingParams paging, SortParams sort, ProjectionParams projection)
        {
            return await GetPageByFilterAndProjectionAsync(correlationId, ComposeFilter(filter), paging, ComposeSort(sort), projection);
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
