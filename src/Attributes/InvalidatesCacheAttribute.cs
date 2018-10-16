using System;

namespace Como.WebApi.Caching.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class InvalidatesCacheAttribute : Attribute
    {
        public string[] CacheGroups { get; }
        
        public InvalidatesCacheAttribute(params string[] cacheGroups)
        {
            CacheGroups = cacheGroups;        
        }        
    }
}