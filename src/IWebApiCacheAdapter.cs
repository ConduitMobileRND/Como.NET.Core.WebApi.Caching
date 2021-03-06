using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Como.WebApi.Caching
{
    public interface IWebApiCacheAdapter
    {
        /// <summary>
        ///     Remove a possibly cached method result from the cache
        /// </summary>
        Task InvalidateCachedMethodResults(IList<MethodInvalidationParameters> methodParameters);

        /// <summary>
        ///     Try to fetch a possibly cached method result
        /// </summary>
        Task<CacheGetResult> GetOrUpdate(CacheMethodParameters parameters, Func<Task<IActionResult>> onCacheMiss);
    }
}