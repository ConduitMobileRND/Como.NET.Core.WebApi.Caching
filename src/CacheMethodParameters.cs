using System;
using System.Collections.Generic;

namespace Como.WebApi.Caching
{
    public class CacheMethodParameters
    {
        public CacheMethodParameters(
            string methodName, string scopeName, string scopeValue, IDictionary<string, object> parameters,
            TimeSpan? expirationTime, bool isSlidingExpiration)
        {
            MethodName = methodName;
            ScopeName = scopeName;
            Parameters = parameters;
            ExpirationTime = expirationTime;
            IsSlidingExpiration = isSlidingExpiration;
            ScopeValue = scopeValue;
        }

        public string MethodName { get; }
        public string ScopeName { get; }
        public string ScopeValue { get; }
        public IDictionary<string, object> Parameters { get; }
        public TimeSpan? ExpirationTime { get; }
        public bool IsSlidingExpiration { get; }
    }
}