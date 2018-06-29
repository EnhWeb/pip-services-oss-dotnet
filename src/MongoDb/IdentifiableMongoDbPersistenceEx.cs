using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

using PipServices.Commons.Config;
using PipServices.Commons.Convert;
using PipServices.Commons.Data;
using PipServices.Commons.Reflect;
using PipServices.Data;

namespace PipServices.Oss.MongoDb
{
    // This is temporary extended persistence class to verify performance
    public class IdentifiableMongoDbPersistenceEx<T, K> : IdentifiableMongoDbPersistence<T, K>
        where T : IIdentifiable<K>
        where K : class
    {
        public IdentifiableMongoDbPersistenceEx(string collectionName)
            : base(collectionName)
        { }

        public override async Task<DataPage<object>> GetPageByFilterAndProjectionAsync(string correlationId, FilterDefinition<T> filterDefinition, PagingParams paging = null, SortDefinition<T> sortDefinition = null, ProjectionParams projection = null)
        {
            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<T>();
            var renderedFilter = filterDefinition.Render(documentSerializer, BsonSerializer.SerializerRegistry);

            var query = _collection.Find(filterDefinition);
            if (sortDefinition != null)
            {
                query = query.Sort(sortDefinition);
            }

            var projectionBuilder = Builders<T>.Projection;
            var projectionDefinition = CreateProjectionDefinition(projection, projectionBuilder);

            paging = paging ?? new PagingParams();
            var skip = paging.GetSkip(0);
            var take = paging.GetTake(_maxPageSize);

            var count = paging.Total ? (long?)await query.CountAsync() : null;

            var result = new DataPage<object>()
            {
                Data = new List<object>(),
                Total = count
            };

            using (var cursor = await query.Project(projectionDefinition).Skip((int)skip).Limit((int)take).ToCursorAsync())
            {
                while (await cursor.MoveNextAsync())
                {
                    foreach (var doc in cursor.Current)
                    {
                        if (doc.ElementCount != 0)
                        {
                            result.Data.Add(BsonSerializer.Deserialize<object>(doc));
                        }
                    }
                }
            }

            _logger.Trace(correlationId, $"Retrieved {result.Total} from {_collection} with projection fields = '{StringConverter.ToString(projection)}'");

            return result;
        }

        public override async Task<object> GetOneByIdAsync(string correlationId, K id, ProjectionParams projection)
        {
            var builder = Builders<T>.Filter;
            var filter = builder.Eq(x => x.Id, id);

            var projectionBuilder = Builders<T>.Projection;
            var projectionDefinition = CreateProjectionDefinition(projection, projectionBuilder);

            var result = await _collection.Find(filter).Project(projectionDefinition).FirstOrDefaultAsync();

            if (result == null)
            {
                _logger.Trace(correlationId, "Nothing found from {0} with id = {1} and projection fields '{2}'", _collectionName, id, StringConverter.ToString(projection));
                return null;
            }

            if (result.ElementCount == 0)
            {
                _logger.Trace(correlationId, "Retrieved from {0} with id = {1}, but projection is not valid '{2}'", _collectionName, id, StringConverter.ToString(projection));
                return null;
            }

            _logger.Trace(correlationId, "Retrieved from {0} with id = {1} and projection fields '{2}'", _collectionName, id, StringConverter.ToString(projection));

            return BsonSerializer.Deserialize<object>(result);
        }
    }
}