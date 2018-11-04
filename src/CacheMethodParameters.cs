using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace Como.WebApi.Caching
{
    public class CacheMethodParameters
    {
        public CacheMethodParameters(
            string methodName, string scopeName, string scopeValue, IDictionary<string, object> parameters,
            string outputContentType,
            TimeSpan? expirationTime, bool isSlidingExpiration)
        {
            MethodName = methodName;
            ScopeName = scopeName;
            Parameters = parameters;
            OutputContentType = outputContentType;
            ExpirationTime = expirationTime;
            IsSlidingExpiration = isSlidingExpiration;
            ScopeValue = scopeValue;
        }

        public string MethodName { get; }
        public string ScopeName { get; }
        public string ScopeValue { get; }
        public IDictionary<string, object> Parameters { get; }
        public string OutputContentType { get; }
        public TimeSpan? ExpirationTime { get; }
        public bool IsSlidingExpiration { get; }
    }
}