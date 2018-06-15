using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

using PipServices.Commons.Data;

namespace PipServices.Oss.MockDb
{
    /// <summary>
    /// Abstract Mock DB Persistence (keep data in memory)
    /// </summary>
    /// <typeparam name="T">The class that contains "Id" property.</typeparam>
    public abstract class AbstractMockDbPersistence<T>
        where T : class
    {
        protected const int MaxPageSize = 100;
        protected object _lock = new object();
        protected Dictionary<string, T> _objects = new Dictionary<string, T>();

        public virtual async Task<T> CreateAsync(string correlationId, T @object)
        {
            dynamic obj = @object;
            obj.Id = obj.Id ?? IdGenerator.NextLong();

            lock (_lock)
            {
                _objects[obj.Id] = @object;
            }

            return await Task.FromResult(@object);
        }

        public virtual async Task ClearAsync()
        {
            lock (_lock)
            {
                _objects.Clear();
            }

            await Task.Delay(0);
        }

        public virtual async Task<T> DeleteAsync(string correlationId, string id)
        {
            T result = null;

            lock (_lock)
            {
                _objects.TryGetValue(id, out result);
                if (result != null)
                {
                    _objects.Remove(id);
                }
            }

            return await Task.FromResult(result);
        }


        public virtual Task<DataPage<T>> GetAsync(string correlationId, FilterParams filter, PagingParams paging)
        {
            filter = filter ?? new FilterParams();

            lock (_lock)
            {
                var foundObjects = new List<T>();

                foreach (dynamic obj in _objects.Values)
                {
                    var isFiltered = false;

                    foreach (var filterKey in filter.Keys)
                    {
                        if (filterKey.Equals("ids"))
                        {
                            var ids = filter.GetAsNullableString("ids");

                            if (ids != null && !ids.Contains(obj.Id))
                            {
                                isFiltered = true;
                                break;
                            }

                            continue;
                        }

                        var propertyValue = GetObjectFieldValue(obj, filterKey);

                        if (propertyValue != null)
                        {
                            var propertyValues = propertyValue as List<object>;

                            if (propertyValues != null)
                            {
                                if (!ContainsFilterValue(propertyValues, filter[filterKey]))
                                {
                                    isFiltered = true;
                                    break;
                                }
                            }
                            else if (!propertyValue.Equals(filter[filterKey]))
                            {
                                isFiltered = true;
                                break;
                            }
                        }
                    }

                    if (!isFiltered)
                    {
                        foundObjects.Add(obj);
                    }
                }

                paging = paging ?? new PagingParams();
                var skip = paging.GetSkip(0);
                var take = paging.GetTake(MaxPageSize);
                var page = foundObjects.Skip((int)skip).Take((int)take).ToList();
                var total = foundObjects.Count;

                return Task.FromResult(new DataPage<T>(page, total));
            }
        }

        public virtual async Task<DataPage<object>> GetAsync(string correlationId, FilterParams filter, PagingParams paging, ProjectionParams projection)
        {
            projection = projection ?? new ProjectionParams();

            var objectsResult = await GetAsync(correlationId, filter, paging);

            var result = new DataPage<object>()
            {
                Data = new List<object>(),
                Total = objectsResult.Total
            };

            foreach (var order in objectsResult.Data)
            {
                if (projection.Count == 0)
                {
                    result.Data.Add(order);
                }
                else
                {
                    dynamic customObject = new ExpandoObject();
                    var customObjectMap = customObject as IDictionary<string, object>;

                    foreach (var projectionField in projection)
                    {
                        customObjectMap = GetCustomObjectMap(order, customObjectMap, projectionField);
                    }

                    if (customObjectMap.Count > 0)
                    {
                        result.Data.Add(customObject);
                    }
                }
            }

            return result;
        }

        public virtual async Task<T> GetByIdAsync(string correlationId, string id)
        {
            T result = null;

            lock (_lock)
            {
                _objects.TryGetValue(id, out result);
            }

            return await Task.FromResult(result);
        }

        public virtual async Task<object> GetByIdAsync(string correlationId, string id, ProjectionParams projection)
        {
            projection = projection ?? new ProjectionParams();

            T obj = null;

            lock (_lock)
            {
                _objects.TryGetValue(id, out obj);
            }

            if (obj != null)
            {
                if (projection.Count == 0)
                {
                    return await Task.FromResult(obj);
                }

                dynamic customObject = new ExpandoObject();
                var customObjectMap = customObject as IDictionary<string, object>;

                foreach (var projectionField in projection)
                {
                    customObjectMap = GetCustomObjectMap(obj, customObjectMap, projectionField);
                }

                if (customObjectMap.Count > 0)
                {
                    return await Task.FromResult(customObject);
                }
            }

            return null;
        }

        public virtual async Task<T> UpdateAsync(string correlationId, T @object)
        {
            dynamic obj = @object;

            lock (_lock)
            {
                _objects[obj.Id] = @object;
            }

            return await Task.FromResult(@object);
        }

        public virtual async Task<T> ModifyAsync(string correlationId, string id, AnyValueMap updateMap)
        {
            updateMap = updateMap ?? new AnyValueMap();

            var order = await GetByIdAsync(correlationId, id);

            foreach (var propertyName in updateMap.Keys)
            {
                ModifyObjectFields(order, propertyName, updateMap[propertyName]);
            }

            return order;
        }


        #region Helper Methods

        protected virtual IDictionary<string, object> GetCustomObjectMap(object parentObject, IDictionary<string, object> customObjectMap, string projectionField)
        {
            if (string.IsNullOrWhiteSpace(projectionField))
            {
                return customObjectMap;
            }

            var innerPropertyFieldIndex = projectionField.IndexOf('.');

            if (innerPropertyFieldIndex > 0)
            {
                var innerPropertyField = projectionField.Substring(0, innerPropertyFieldIndex);
                var subPropertyField = projectionField.Substring(innerPropertyFieldIndex + 1);

                var propertyInfo = parentObject.GetType()
                    .GetProperties()
                    .Where(p => Attribute.IsDefined(p, typeof(DataMemberAttribute)))
                    .FirstOrDefault(p => ((DataMemberAttribute)Attribute.GetCustomAttribute(p, typeof(DataMemberAttribute)))
                    .Name.Equals(innerPropertyField));

                if (propertyInfo != null)
                {
                    var innerObject = propertyInfo.GetValue(parentObject);

                    if (innerObject != null)
                    {
                        if (!customObjectMap.ContainsKey(innerPropertyField))
                        {
                            dynamic innerCustomObject = new ExpandoObject();
                            customObjectMap[innerPropertyField] = innerCustomObject;
                        }

                        var innerCustomObjectMap = customObjectMap[innerPropertyField] as IDictionary<string, object>;

                        GetCustomObjectMap(innerObject, innerCustomObjectMap, subPropertyField);

                        return customObjectMap;
                    }
                }
            }
            else
            {
                var propertyInfo = parentObject.GetType()
                    .GetProperties()
                    .Where(p => Attribute.IsDefined(p, typeof(DataMemberAttribute)))
                    .FirstOrDefault(p => ((DataMemberAttribute)Attribute.GetCustomAttribute(p, typeof(DataMemberAttribute)))
                    .Name.Equals(projectionField));

                if (propertyInfo != null)
                {
                    var value = propertyInfo.GetValue(parentObject);

                    if (value != null)
                    {
                        customObjectMap[projectionField] = value;
                    }
                }
            }

            return customObjectMap;
        }

        protected virtual void ModifyObjectFields(object parentObject, string propertyName, object propertyValue)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                return;
            }

            var innerPropertyFieldIndex = propertyName.IndexOf('.');

            if (innerPropertyFieldIndex > 0)
            {
                var innerPropertyField = propertyName.Substring(0, innerPropertyFieldIndex);
                var subPropertyField = propertyName.Substring(innerPropertyFieldIndex + 1);

                var propertyInfo = parentObject.GetType()
                    .GetProperties()
                    .Where(p => Attribute.IsDefined(p, typeof(DataMemberAttribute)))
                    .FirstOrDefault(p => ((DataMemberAttribute)Attribute.GetCustomAttribute(p, typeof(DataMemberAttribute)))
                    .Name.Equals(innerPropertyField));

                if (propertyInfo != null)
                {
                    var innerObject = propertyInfo.GetValue(parentObject);

                    if (innerObject != null)
                    {
                        ModifyObjectFields(innerObject, subPropertyField, propertyValue);
                    }
                }
            }
            else
            {
                var propertyInfo = parentObject.GetType()
                    .GetProperties()
                    .Where(p => Attribute.IsDefined(p, typeof(DataMemberAttribute)))
                    .FirstOrDefault(p => ((DataMemberAttribute)Attribute.GetCustomAttribute(p, typeof(DataMemberAttribute)))
                    .Name.Equals(propertyName));

                if (propertyInfo != null)
                {
                    propertyInfo.SetValue(parentObject, propertyValue);
                }
            }
        }

        protected virtual object GetObjectFieldValue(object parentObject, string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                return null;
            }

            var innerPropertyFieldIndex = propertyName.IndexOf('.');

            if (innerPropertyFieldIndex > 0)
            {
                var innerPropertyField = propertyName.Substring(0, innerPropertyFieldIndex);
                var subPropertyField = propertyName.Substring(innerPropertyFieldIndex + 1);

                var propertyInfo = parentObject.GetType()
                    .GetProperties()
                    .Where(p => Attribute.IsDefined(p, typeof(DataMemberAttribute)))
                    .FirstOrDefault(p => ((DataMemberAttribute)Attribute.GetCustomAttribute(p, typeof(DataMemberAttribute)))
                    .Name.Equals(innerPropertyField));

                if (propertyInfo != null)
                {
                    var innerObject = propertyInfo.GetValue(parentObject);

                    if (innerObject != null)
                    {
                        if (innerObject is IEnumerable)
                        {
                            var innerResult = new List<object>();

                            foreach (var obj in (innerObject as IEnumerable))
                            {
                                innerResult.Add(GetObjectFieldValue(obj, subPropertyField));
                            }

                            return innerResult;
                        }
                        else
                        {
                            return GetObjectFieldValue(innerObject, subPropertyField);
                        }
                    }
                }
            }
            else
            {
                var propertyInfo = parentObject.GetType()
                    .GetProperties()
                    .Where(p => Attribute.IsDefined(p, typeof(DataMemberAttribute)))
                    .FirstOrDefault(p => ((DataMemberAttribute)Attribute.GetCustomAttribute(p, typeof(DataMemberAttribute)))
                    .Name.Equals(propertyName));

                if (propertyInfo != null)
                {
                    return propertyInfo.GetValue(parentObject);
                }
            }

            return null;
        }

        protected virtual bool ContainsFilterValue(List<object> propertyValues, string filterValue)
        {
            if (propertyValues == null || propertyValues.Count == 0)
            {
                return false;
            }

            var result = false;

            foreach (var value in propertyValues)
            {
                var values = value as List<object>;

                if (values != null)
                {
                    result = ContainsFilterValue(values, filterValue);
                }
                else if (value.Equals(filterValue))
                {
                    result = true;
                }
            }

            return result;
        }

        #endregion
    }

}