using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

using PipServices.Commons.Config;
using PipServices.Commons.Convert;
using PipServices.Commons.Data;
using PipServices.Commons.Reflect;
using PipServices.Data;

namespace PipServices.Oss.MongoDb
{
    public class IdentifiableMongoDbPersistence<T, K> : MongoDbPersistence<T>, IWriter<T, K>, IGetter<T, K>, ISetter<T>
        where T : IIdentifiable<K>
        where K : class
    {
        protected int _maxPageSize = 100;

        public IdentifiableMongoDbPersistence(string collectionName)
            : base(collectionName)
        { }

        public override void Configure(ConfigParams config)
        {
            base.Configure(config);

            _maxPageSize = config.GetAsIntegerWithDefault("options.max_page_size", _maxPageSize);
        }

        public async Task<DataPage<T>> GetPageByFilterAsync(string correlationId, FilterDefinition<T> filterDefinition,
            PagingParams paging = null, SortDefinition<T> sortDefinition = null)
        {
            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<T>();
            var renderedFilter = filterDefinition.Render(documentSerializer, BsonSerializer.SerializerRegistry);

            var query = _collection.Find(filterDefinition);
            if (sortDefinition != null)
                query = query.Sort(sortDefinition);

            paging = paging ?? new PagingParams();
            var skip = paging.GetSkip(0);
            var take = paging.GetTake(_maxPageSize);

            var count = paging.Total ? (long?)await query.CountAsync() : null;
            var items = await query.Skip((int)skip).Limit((int)take).ToListAsync();

            _logger.Trace(correlationId, $"Retrieved {items.Count} from {_collection}");

            return new DataPage<T>()
            {
                Data = items,
                Total = count
            };
        }

        public async Task<DataPage<object>> GetPageByFilterAndProjectionAsync(string correlationId, FilterDefinition<T> filterDefinition,
            PagingParams paging = null, SortDefinition<T> sortDefinition = null, ProjectionParams projection = null)
        {
            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<T>();
            var renderedFilter = filterDefinition.Render(documentSerializer, BsonSerializer.SerializerRegistry);

            var query = _collection.Find(filterDefinition);
            if (sortDefinition != null)
            {
                query = query.Sort(sortDefinition);
            }

            projection = projection ?? new ProjectionParams();
            var projectionBuilder = Builders<T>.Projection;
            var projectionDefinition = projectionBuilder.Combine(projection.Select(field => projectionBuilder.Include(field))).Exclude("_id");

            paging = paging ?? new PagingParams();
            var skip = paging.GetSkip(0);
            var take = paging.GetTake(_maxPageSize);

            var count = paging.Total ? (long?)await query.CountAsync() : null;
            var items = await query.Project(projectionDefinition).Skip((int)skip).Limit((int)take).ToListAsync();

            var result = new DataPage<object>()
            {
                Data = new List<object>(),
                Total = count
            };

            foreach (var item in items)
            {
                // Maybe we could do another check if item is empty?
                if (item.Elements.Count() > 0)
                {
                    // Convert to JSON to fix issue with ISODate
                    var jsonString = JsonConverter.ToJson(BsonTypeMapper.MapToDotNetValue(item));

                    result.Data.Add(JsonConverter.FromJson<ExpandoObject>(jsonString));
                }
            }

            result.Total = result.Data.Count;

            _logger.Trace(correlationId, $"Retrieved {result.Total} from {_collection} with projection fields = '{StringConverter.ToString(projection)}'");

            return result;
        }

        public async Task<List<T>> GetListByFilterAsync(string correlationId, FilterDefinition<T> filterDefinition,
            SortDefinition<T> sortDefinition = null)
        {
            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<T>();
            var renderedFilter = filterDefinition.Render(documentSerializer, BsonSerializer.SerializerRegistry);

            var query = _collection.Find(filterDefinition);
            if (sortDefinition != null)
                query = query.Sort(sortDefinition);

            var items = await query.ToListAsync();

            _logger.Trace(correlationId, $"Retrieved {items.Count} from {_collection}");

            return items;
        }

        public async Task<List<T>> GetListByIdsAsync(string correlationId, K[] ids)
        {
            
            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<T>();
            var builder = Builders<T>.Filter;
            var filterDefinition = builder.In(x => x.Id, ids);
            var renderedFilter = filterDefinition.Render(documentSerializer, BsonSerializer.SerializerRegistry);

            var query = _collection.Find(filterDefinition);
            var items = await query.ToListAsync();

            _logger.Trace(correlationId, $"Retrieved {items.Count} from {_collection}");

            return items;
        }


        public async Task<T> GetOneByIdAsync(string correlationId, K id)
        {
            var builder = Builders<T>.Filter;
            var filter = builder.Eq(x => x.Id, id);
            var result = await _collection.Find(filter).FirstOrDefaultAsync();

            if (result == null)
            {
                _logger.Trace(correlationId, "Nothing found from {0} with id = {1}", _collectionName, id);
                return default(T);
            }

            _logger.Trace(correlationId, "Retrieved from {0} with id = {1}", _collectionName, id);

            return result;
        }

        public async Task<object> GetOneByIdAsync(string correlationId, K id, ProjectionParams projection)
        {
            var builder = Builders<T>.Filter;
            var filter = builder.Eq(x => x.Id, id);

            projection = projection ?? new ProjectionParams();
            var projectionBuilder = Builders<T>.Projection;
            var projectionDefinition = projectionBuilder.Combine(projection.Select(field => projectionBuilder.Include(field))).Exclude("_id");

            var result = await _collection.Find(filter).Project(projectionDefinition).FirstOrDefaultAsync();

            if (result == null)
            {
                _logger.Trace(correlationId, "Nothing found from {0} with id = {1} and projection fields '{2}'", _collectionName, id, StringConverter.ToString(projection));
                return null;
            }

            if (result.Elements.Count() == 0)
            {
                _logger.Trace(correlationId, "Retrieved from {0} with id = {1}, but projection is not valid '{2}'", _collectionName, id, StringConverter.ToString(projection));
                return null;
            }

            _logger.Trace(correlationId, "Retrieved from {0} with id = {1} and projection fields '{2}'", _collectionName, id, StringConverter.ToString(projection));

            // Convert to JSON to fix issue with ISODate
            var jsonString = JsonConverter.ToJson(BsonTypeMapper.MapToDotNetValue(result));

            // convert result to dynamic object
            return JsonConverter.FromJson<ExpandoObject>(jsonString);
        }

        public async Task<T> GetOneRandomAsync(string correlationId, FilterDefinition<T> filterDefinition)
        {
            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<T>();
            var renderedFilter = filterDefinition.Render(documentSerializer, BsonSerializer.SerializerRegistry);

            var count = (int)_collection.Count(filterDefinition);

            if (count <= 0)
            {
                _logger.Trace(correlationId, "Nothing found for filter {0}", renderedFilter.ToString());
                return default(T);
            }

            var randomIndex = new Random().Next(0, count - 1);

            var result = await _collection.Find(filterDefinition).Skip(randomIndex).FirstOrDefaultAsync();

            _logger.Trace(correlationId, "Retrieved randomly from {0} with id = {1}", _collectionName, result.Id);

            return result;
        }

        public async Task<T> CreateAsync(string correlationId, T item)
        {
            var identifiable = item as IStringIdentifiable;
            if (identifiable != null && item.Id == null)
                ObjectWriter.SetProperty(item, nameof(item.Id), IdGenerator.NextLong());

            await _collection.InsertOneAsync(item, null);

            _logger.Trace(correlationId, "Created in {0} with id = {1}", _collectionName, item.Id);

            return item;
        }

        public async Task<T> SetAsync(string correlationId, T item)
        {
            var identifiable = item as IIdentifiable<K>;
            if (identifiable == null || item.Id == null)
                return default(T);

            var filter = Builders<T>.Filter.Eq(x => x.Id, identifiable.Id);
            var options = new FindOneAndReplaceOptions<T>
            {
                ReturnDocument = ReturnDocument.After,
                IsUpsert = true
            };
            var result = await _collection.FindOneAndReplaceAsync(filter, item, options);

            _logger.Trace(correlationId, "Set in {0} with id = {1}", _collectionName, item.Id);

            return result;
        }

        public async Task<T> UpdateAsync(string correlationId, T item)
        {
            var identifiable = item as IIdentifiable<K>;
            if (identifiable == null || item.Id == null)
                return default(T);

            var filter = Builders<T>.Filter.Eq(x => x.Id, identifiable.Id);
            var options = new FindOneAndReplaceOptions<T>
            {
                ReturnDocument = ReturnDocument.After,
                IsUpsert = false
            };
            var result = await _collection.FindOneAndReplaceAsync(filter, item, options);

            _logger.Trace(correlationId, "Update in {0} with id = {1}", _collectionName, item.Id);

            return result;
        }

        public async Task<T> DeleteByIdAsync(string correlationId, K id)
        {
            var filter = Builders<T>.Filter.Eq(x => x.Id, id);
            var options = new FindOneAndDeleteOptions<T>();
            var result = await _collection.FindOneAndDeleteAsync(filter, options);

            _logger.Trace(correlationId, "Deleted from {0} with id = {1}", _collectionName, id);

            return result;
        }

        public async Task DeleteByFilterAsync(string correlationId, FilterDefinition<T> filterDefinition)
        {
            var result = await _collection.DeleteManyAsync(filterDefinition);

            _logger.Trace(correlationId, $"Deleted {result.DeletedCount} from {_collection}");
        }

        public async Task DeleteByIdsAsync(string correlationId, K[] ids)
        {
            var filterDefinition = Builders<T>.Filter.In(x => x.Id, ids);

            var result = await _collection.DeleteManyAsync(filterDefinition);

            _logger.Trace(correlationId, $"Deleted {result.DeletedCount} from {_collection}");
        }

    }
}