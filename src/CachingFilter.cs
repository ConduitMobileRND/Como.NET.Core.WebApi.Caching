using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Como.WebApi.Caching.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Como.WebApi.Caching
{
    /// <summary>
    ///     WebApi server-side distributed response caching middleware.
    ///     <para>
    ///         NOTE:
    ///         make sure to register this filter only after you registered the authentication &amp; authorization filters
    ///         in order to prevent a cache speculation and other forms of attacks.
    ///     </para>
    /// </summary>
    public class CachingFilter : IAsyncActionFilter
    {
        private static IDictionary<string, List<MethodInfo>> _cachedMethodsPerGroup;
        private readonly IWebApiCacheAdapter _cacheAdapter;
        private readonly ICacheParametersResolver _cacheParametersResolver;

        public CachingFilter(IWebApiCacheAdapter cacheAdapterAdapter,
            ICacheParametersResolver cacheParametersResolver)
        {
            _cacheAdapter = cacheAdapterAdapter;
            _cacheParametersResolver = cacheParametersResolver;
            _cachedMethodsPerGroup = _cachedMethodsPerGroup ?? GetCachedMethodsPerGroup();
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var actionDescriptor = (ControllerActionDescriptor) context.ActionDescriptor;
            var actionMethod = actionDescriptor.MethodInfo;
            var invalidatesAttribute = actionMethod.GetCustomAttribute<InvalidatesCacheAttribute>();
            var delayedInvalidatesAttribute = actionMethod.GetCustomAttribute<DelayedInvalidatesCacheAttribute>();

            var cachedAttribute = actionMethod.GetCustomAttribute<CachedAttribute>();
            if (cachedAttribute != null && (invalidatesAttribute != null || delayedInvalidatesAttribute != null))
            {
                throw new InvalidOperationException("Method can either have " +
                                                    $"{nameof(CachedAttribute)} " +
                                                    $"or {nameof(InvalidatesCacheAttribute)}" +
                                                    $"/{nameof(DelayedInvalidatesCacheAttribute)} but not both! " +
                                                    $"[method name: {actionMethod.GetUniqueIdentifier()}]");
            }

            if (cachedAttribute != null)
            {
                if (context.HttpContext.Request.Method != HttpMethods.Get)
                {
                    throw new InvalidOperationException("Non HTTP Get methods should not be cached!");
                }

                await HandleCachedAttribute(cachedAttribute, actionMethod, context, next);
            }
            else
            {
                await next();
            }

            if (invalidatesAttribute != null)
            {
                await HandleInvalidatesAttribute(invalidatesAttribute, context);
            }

            if (delayedInvalidatesAttribute != null)
            {
                HandleDelayedInvalidatesAttribute(delayedInvalidatesAttribute, context);
            }
        }

        private async Task HandleInvalidatesAttribute(InvalidatesCacheAttribute attribute,
            ActionExecutingContext context)
        {
            var invalidationParameters = new List<MethodInvalidationParameters>();
            foreach (var cacheGroup in attribute.CacheGroups)
            {
                if (!_cachedMethodsPerGroup.TryGetValue(cacheGroup, out var methods))
                {
                    continue;
                }

                foreach (var cachedMethod in methods)
                {
                    var targetMethodCacheAttribute = cachedMethod.GetCustomAttribute<CachedAttribute>();
                    var scopeValue =
                        _cacheParametersResolver.ResolveScopeValue(targetMethodCacheAttribute.ScopeName, context);
                    invalidationParameters.Add(new MethodInvalidationParameters(
                        cachedMethod.GetUniqueIdentifier(), targetMethodCacheAttribute.ScopeName, scopeValue));
                }
            }

            await _cacheAdapter.InvalidateCachedMethodResults(invalidationParameters);
        }

        private async Task HandleCachedAttribute(
            CachedAttribute attribute, MethodInfo actionMethod,
            ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (UserAgentSpecifiedNoCache(context.HttpContext.Request))
            {
                await next();
                return;
            }

            var scopeValue = _cacheParametersResolver.ResolveScopeValue(attribute.ScopeName, context);
            var methodParameters = new CacheMethodParameters(
                actionMethod.GetUniqueIdentifier(),
                attribute.ScopeName,
                scopeValue,
                context.ActionArguments,
                attribute.ExpireAfterTimeSpan ?? _cacheParametersResolver.DefaultExpiration,
                attribute.SlidingExpiration);
            var cacheGetResult = await _cacheAdapter.GetOrUpdate(methodParameters, () => OnCacheMiss(next));
            if (cacheGetResult.Hit)
            {
                context.Result = cacheGetResult.ValueFromCache;
            }
        }

        private async Task<IActionResult> OnCacheMiss(ActionExecutionDelegate next)
        {
            var executedAction = await next();
            if (executedAction.Exception != null)
            {
                return null;
            }

            int httpStatusCode;
            switch (executedAction.Result)
            {
                case ObjectResult objectResult:
                {
                    httpStatusCode = objectResult.StatusCode ?? (int) HttpStatusCode.OK;
                    break;
                }
                case StatusCodeResult statusCodeResult:
                {
                    httpStatusCode = statusCodeResult.StatusCode;
                    break;
                }
                default:
                    throw new UnsupportedContentTypeException("unsupported WebApi method result type!");
            }

            if (httpStatusCode < 200 || httpStatusCode > 299)
            {
                return null;
            }

            return executedAction.Result;
        }

        private bool UserAgentSpecifiedNoCache(HttpRequest request)
        {
            if (!request.Headers.ContainsKey("Cache-Control"))
            {
                return false;
            }

            // see https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Cache-Control
            switch (request.Headers["Cache-Control"])
            {
                case "no-cache":
                case "no-store":
                case "private":
                    return true;
            }

            return false;
        }

        private IDictionary<string, List<MethodInfo>> GetCachedMethodsPerGroup()
        {
            var controllers = _cacheParametersResolver.GetWebApiControllers();
            var methodsWithCachedAttribute = new List<(MethodInfo, CachedAttribute)>();
            foreach (var controller in controllers)
            {
                var methods = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public);
                var controllerCachedMethods = methods
                    .Select(methodInfo => (methodInfo, methodInfo.GetCustomAttribute<CachedAttribute>()))
                    .Where(x => x.Item2 != null);
                methodsWithCachedAttribute.AddRange(controllerCachedMethods);
            }

            var result = new Dictionary<string, List<MethodInfo>>();
            foreach (var (method, attribute) in methodsWithCachedAttribute)
            {
                foreach (var group in attribute.CacheGroups)
                {
                    if (!result.ContainsKey(group))
                    {
                        result.Add(group, new List<MethodInfo>());
                    }

                    result[group].Add(method);
                }
            }

            return result;
        }

        private void HandleDelayedInvalidatesAttribute(DelayedInvalidatesCacheAttribute attribute,
            ActionExecutingContext context)
        {
            var invalidationParameters = new List<MethodInvalidationParameters>();
            foreach (var cacheGroup in attribute.CacheGroups)
            {
                if (!_cachedMethodsPerGroup.TryGetValue(cacheGroup, out var methods))
                {
                    continue;
                }

                foreach (var cachedMethod in methods)
                {
                    var targetMethodCacheAttribute = cachedMethod.GetCustomAttribute<CachedAttribute>();
                    var scopeValue =
                        _cacheParametersResolver.ResolveScopeValue(targetMethodCacheAttribute.ScopeName, context);
                    invalidationParameters.Add(new MethodInvalidationParameters(
                        cachedMethod.GetUniqueIdentifier(), targetMethodCacheAttribute.ScopeName, scopeValue));
                }
            }

            _cacheAdapter.InvalidateCachedMethodResultsWithDelay(invalidationParameters, attribute.InvalidatesAfter);
        }
    }
}