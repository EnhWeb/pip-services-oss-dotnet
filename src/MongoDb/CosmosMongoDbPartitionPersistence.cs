using PipServices.Commons.Data;

using System.Threading.Tasks;

using MongoDB.Bson;
using MongoDB.Driver;

namespace PipServices.Oss.MongoDb
{
    public class CosmosMongoDbPartitionPersistence<T, K> : IdentifiableMongoDbPersistence<T, K>
        where T : IIdentifiable<K>
        where K : class
    {
        private string _partitionKey;

        public CosmosMongoDbPartitionPersistence(string collectionName, string partitionKey)
            : base(collectionName)
        {
            _partitionKey = partitionKey;
        }

        public override async Task OpenAsync(string correlationId)
        {
            await base.OpenAsync(correlationId);

            if (!await CollectionExistsAsync())
            {
                await CreatePartitionCollectionAsync();
            }
        }

        public override async Task<T> DeleteByIdAsync(string correlationId, K id)
        {
            var builder = Builders<T>.Filter;
            var filter = builder.Empty;

            filter &= builder.Eq(x => x.Id, id);
            filter &= builder.Eq(_partitionKey, GetPartitionKey(id));

            var options = new FindOneAndDeleteOptions<T>();
            var result = await _collection.FindOneAndDeleteAsync(filter, options);

            _logger.Trace(correlationId, $"Deleted from {_collectionName} with id = {id} and partition_key = {_partitionKey}");

            return result;
        }

        public override async Task<T> ModifyByIdAsync(string correlationId, K id, UpdateDefinition<T> updateDefinition)
        {
            if (id == null || updateDefinition == null)
            {
                return default(T);
            }

            var builder = Builders<T>.Filter;
            var filter = builder.Empty;

            filter &= builder.Eq(x => x.Id, id);
            filter &= builder.Eq(_partitionKey, GetPartitionKey(id));

            var result = await ModifyAsync(correlationId, filter, updateDefinition);

            _logger.Trace(correlationId, $"Modified in {_collectionName} with id = {id} and partition_key = {_partitionKey}");

            return result;
        }

        public override async Task<T> UpdateAsync(string correlationId, T item)
        {
            var identifiable = item as IIdentifiable<K>;
            if (identifiable == null || item.Id == null)
            {
                return default(T);
            }

            var builder = Builders<T>.Filter;
            var filter = builder.Empty;

            filter &= builder.Eq(x => x.Id, identifiable.Id);
            filter &= builder.Eq(_partitionKey, GetPartitionKey(identifiable.Id));

            var options = new FindOneAndReplaceOptions<T>
            {
                ReturnDocument = ReturnDocument.After,
                IsUpsert = false
            };
            var result = await _collection.FindOneAndReplaceAsync(filter, item, options);

            _logger.Trace(correlationId, $"Updated in {_collectionName} with id = {identifiable.Id} and partition_key = {_partitionKey}");

            return result;
        }

        public async Task CreatePartitionCollectionAsync()
        {
            try
            {
                // Specific CosmosDB command that creates partition collection (it raises exception for MongoDB)
                await _database.RunCommandAsync(new BsonDocumentCommand<BsonDocument>(new BsonDocument
                {
                    {"shardCollection", $"{_database.DatabaseNamespace.DatabaseName}.{_collectionName}"},
                    {"key", new BsonDocument {{ _partitionKey, "hashed"}}}
                }));
            }
            catch
            {
                // Do nothing
            }
            finally
            {
                _collection = _database.GetCollection<T>(_collectionName);
            }
        }

        protected virtual string GetPartitionKey(K id)
        {
            return string.Empty;
        }
    }
}