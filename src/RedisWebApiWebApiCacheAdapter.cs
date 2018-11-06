using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using StackExchange.Redis;

namespace Como.WebApi.Caching
{
    public class RedisWebApiWebApiCacheAdapter : IWebApiCacheAdapter
    {
        private readonly IConnectionMultiplexer _redisConnectionMultiplexer;
        private readonly SerializationHelper _serializationHelper;

        public RedisWebApiWebApiCacheAdapter(
            IConnectionMultiplexer redisConnectionMultiplexer, 
            SerializationHelper serializationHelper)
        {
            _redisConnectionMultiplexer = redisConnectionMultiplexer;
            _serializationHelper = serializationHelper;
        }

        public async Task InvalidateCachedMethodResults(IList<MethodInvalidationParameters> methodParameters)
        {
            var database = _redisConnectionMultiplexer.GetDatabase();
            var keys = new RedisKey[methodParameters.Count];
            for (var i = 0; i < methodParameters.Count; i++)
            {
                keys[i] = GetCacheKey(
                    methodParameters[i].MethodName,
                    methodParameters[i].ScopeName,
                    methodParameters[i].ScopeValue);
            }

            await database.KeyDeleteAsync(keys);
        }

        public async Task<CacheGetResult> GetOrUpdate(
            CacheMethodParameters parameters, Func<Task<IActionResult>> cacheMissResolver)
        {
            var database = _redisConnectionMultiplexer.GetDatabase();
            var key = GetCacheKey(parameters.MethodName, parameters.ScopeName, parameters.ScopeValue);
            var (statusCodeFieldName, payloadFieldName, contentTypeFieldName) = GetCacheParametersFieldNames(parameters);
            var cached = await database.HashGetAsync(key, new RedisValue[]
            {
                statusCodeFieldName, payloadFieldName, contentTypeFieldName
            });
            var statusCode = cached[0];
            var payload = cached[1];
            var contentType = cached[2];
            if (!statusCode.HasValue)
            {
                var newValue = await cacheMissResolver();
                if (newValue != null)
                {
                    await CacheMethodResult(parameters, statusCodeFieldName, payloadFieldName, contentTypeFieldName, newValue);
                }

                return new CacheGetResult(false, null);
            }

            if (parameters.ExpirationTime.HasValue && parameters.IsSlidingExpiration)
            {
                await database.KeyExpireAsync(key, parameters.ExpirationTime.Value);
            }

            var result = new RawActionResult((int) statusCode, payload, contentType,
                new Dictionary<string, string>
                {
                    ["x-served-from-cache"] = "true"
                });
            return new CacheGetResult(true, result);
        }

        private async Task CacheMethodResult(
            CacheMethodParameters parameters, string statusCodeFieldName, 
            string payloadFieldName, string contentTypeFieldName, IActionResult result)
        {
            var entries = new List<HashEntry>();
            switch (result)
            {
                // methods that does not return an IActionResult might not have the status code field populated:
                case ObjectResult objectResult:
                {
                    var statusCode = objectResult.StatusCode ?? (int) HttpStatusCode.OK;
                    var contentTypeFormatterId = parameters.OutputContentType.Split(';')[0];
                    var payload = await _serializationHelper.Serialize(contentTypeFormatterId, objectResult.Value);                    
                    entries.Add(new HashEntry(statusCodeFieldName, statusCode));
                    entries.Add(new HashEntry(payloadFieldName, payload));
                    entries.Add(new HashEntry(contentTypeFieldName, parameters.OutputContentType));
                    break;
                }
                case StatusCodeResult statusCodeResult:
                {
                    var statusCode = statusCodeResult.StatusCode;
                    entries.Add(new HashEntry(statusCodeFieldName, statusCode));
                    break;
                }
                default:
                    throw new UnsupportedContentTypeException("unsupported WebApi method result type!");
            }

            var key = GetCacheKey(parameters.MethodName, parameters.ScopeName, parameters.ScopeValue);
            var database = _redisConnectionMultiplexer.GetDatabase();
            await database.HashSetAsync(key, entries.ToArray());
            if (parameters.ExpirationTime.HasValue)
            {
                await database.KeyExpireAsync(key, parameters.ExpirationTime.Value);
            }
        }


        private string GetCacheKey(string methodName, string scopeName, string scopeValue)
        {
            return $"{methodName}:{scopeName}:{scopeValue}";
        }

        private (string statusCodeFieldName, string payloadFieldNamem, string contentTypeFieldName)
            GetCacheParametersFieldNames(CacheMethodParameters parameters)
        {
            var parametersHash = _serializationHelper.ComputeHash(parameters.Parameters);
            return ($"{parametersHash}:statusCode", $"{parametersHash}:payload", $"{parametersHash}:contentType");
        }

    }
}