using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Como.WebApi.Caching
{
    public interface ICacheParametersResolver
    {
        string ResolveScopeValue(string scopeName, ActionExecutingContext context);
        TimeSpan? DefaultExpiration { get; }
        ICollection<Type> GetWebApiControllers();
    }
}