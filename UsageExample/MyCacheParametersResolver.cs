using System;
using System.Collections.Generic;
using System.Linq;
using Como.WebApi.Caching;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace UsageExample
{
    public class MyCacheParametersResolver : ICacheParametersResolver
    {
        public string ResolveScopeValue(string scopeName, ActionExecutingContext context)
        {
            switch (scopeName)
            {
                case MyCacheConstants.CacheScopePerUser:
                    return context.HttpContext.Request.Query["UserId"];
                default:
                    return string.Empty;
            }
        }

        // default expiration of an item if attribute does not specify another value:
        public TimeSpan? DefaultExpiration => TimeSpan.FromDays(1);

        // return the controllers in your WebApi assembly
        public ICollection<Type> GetWebApiControllers()
        {
            var controllerBaseType = typeof(ControllerBase);
            return typeof(MyCacheParametersResolver).Assembly.GetTypes()
                .Where(t => controllerBaseType.IsAssignableFrom(t)).ToList();
        }
    }
}