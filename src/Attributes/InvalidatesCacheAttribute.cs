using System;

namespace Como.WebApi.Caching.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class InvalidatesCacheAttribute : Attribute
    {
        public InvalidatesCacheAttribute(params string[] cacheGroups)
        {
            CacheGroups = cacheGroups;
        }

        public string[] CacheGroups { get; }
    }
}