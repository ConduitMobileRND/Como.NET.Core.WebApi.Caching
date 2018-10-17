using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Como.WebApi.Caching
{
    public interface ICacheParametersResolver
    {
        TimeSpan? DefaultExpiration { get; }
        string ResolveScopeValue(string scopeName, ActionExecutingContext context);
        ICollection<Type> GetWebApiControllers();
    }
}