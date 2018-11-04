using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Como.WebApi.Caching
{
    public class RedisWebApiWebApiCacheAdapter : IWebApiCacheAdapter, IHostedService, IDisposable
    {
        private readonly JsonOutputFormatter _jsonOutputFormatter;
        private readonly IConnectionMultiplexer _redisConnectionMultiplexer;
        private readonly ILogger<IWebApiCacheAdapter> _logger;
        private readonly ConsecutiveTimer _timer;        
        private readonly ConcurrentQueue<DelayedInvalidationParameters> _methodsToInvalidate;

        public RedisWebApiWebApiCacheAdapter(
            IConnectionMultiplexer redisConnectionMultiplexer,
            IOptions<MvcOptions> mvcOptions,
            ILogger<IWebApiCacheAdapter> logger)
        {
            _timer = new ConsecutiveTimer();
            _timer.OnTick += ProcessDelayedActionsQueue;
            _methodsToInvalidate = new ConcurrentQueue<DelayedInvalidationParameters>();
            _redisConnectionMultiplexer = redisConnectionMultiplexer;                        
            _jsonOutputFormatter = GetJsonOutputFormatterFromMvcOptions(mvcOptions);
            _logger = logger;
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

        public void InvalidateCachedMethodResultsWithDelay(
            IList<MethodInvalidationParameters> methodParameters, TimeSpan delay)
        {
            _methodsToInvalidate.Enqueue(new DelayedInvalidationParameters
            {
                Methods = methodParameters,
                DueTime = DateTime.UtcNow + delay
            });
        }

        public async Task<CacheGetResult> GetOrUpdate(
            CacheMethodParameters parameters, Func<Task<IActionResult>> cacheMissResolver)
        {
            var database = _redisConnectionMultiplexer.GetDatabase();
            var key = GetCacheKey(parameters.MethodName, parameters.ScopeName, parameters.ScopeValue);
            var (statusCodeFieldName, jsonFieldName) = GetCacheParametersFieldNames(parameters);
            var cached = await database.HashGetAsync(key, new RedisValue[] {statusCodeFieldName, jsonFieldName});
            var statusCode = cached[0];
            var json = cached[1];
            if (!statusCode.HasValue)
            {
                var newValue = await cacheMissResolver();
                if (newValue != null)
                {
                    await CacheMethodResult(parameters, statusCodeFieldName, jsonFieldName, newValue);
                }

                return new CacheGetResult(false, null);
            }

            if (parameters.ExpirationTime.HasValue && parameters.IsSlidingExpiration)
            {
                await database.KeyExpireAsync(key, parameters.ExpirationTime.Value);
            }

            var result = new RawActionResult((int) statusCode, json,
                json.HasValue ? RawActionResult.JsonContentType : null,
                new Dictionary<string, string>
                {
                    ["x-served-from-cache"] = "true"
                });
            return new CacheGetResult(true, result);
        }

        private async Task CacheMethodResult(
            CacheMethodParameters parameters, string statusCodeFieldName, string jsonFieldName, IActionResult result)
        {
            var entries = new List<HashEntry>();
            switch (result)
            {
                // methods that does not return an IActionResult might not have the status code field populated:
                case ObjectResult objectResult:
                {
                    var statusCode = objectResult.StatusCode ?? (int) HttpStatusCode.OK;
                    var json = SerializeObjectToJson(objectResult.Value);
                    entries.Add(new HashEntry(statusCodeFieldName, statusCode));
                    entries.Add(new HashEntry(jsonFieldName, json));
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

        private (string statusCodeFieldName, string jsonFieldName)
            GetCacheParametersFieldNames(CacheMethodParameters parameters)
        {
            var parametersHash = _jsonOutputFormatter.ComputeHash(parameters.Parameters);
            return ($"{parametersHash}:statusCode", $"{parametersHash}:json");
        }

        private JsonOutputFormatter GetJsonOutputFormatterFromMvcOptions(IOptions<MvcOptions> mvcOptions)
        {
            foreach (var outputFormatter in mvcOptions.Value.OutputFormatters)
            {
                if (outputFormatter is JsonOutputFormatter jsonOutputFormatter)
                {
                    return jsonOutputFormatter;
                }
            }

            throw new KeyNotFoundException("Couldn't find JSON output formatter!");
        }

        private byte[] SerializeObjectToJson(object obj)
        {
            using (var outputStream = new MemoryStream())
            {
                using (var streamWriter = new StreamWriter(outputStream))
                {
                    _jsonOutputFormatter.WriteObject(streamWriter, obj);
                }

                return outputStream.ToArray();
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer.Start(TimeSpan.FromMilliseconds(100));
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Stop();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        private void ProcessDelayedActionsQueue()
        {
            var dequeuedBuffer = new List<DelayedInvalidationParameters>();
            while (_methodsToInvalidate.TryDequeue(out var item))
            {
                if (DateTime.UtcNow < item.DueTime)
                {
                    dequeuedBuffer.Add(item);
                }
                else
                {
                    try
                    {
                        InvalidateCachedMethodResults(item.Methods).Wait();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Error occurred while trying to invalidate a cached method (delayed)!");
                    }
                }
            }
            dequeuedBuffer.ForEach(_methodsToInvalidate.Enqueue);
        }
    }
}