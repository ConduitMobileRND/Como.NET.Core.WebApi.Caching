using System;

namespace Como.WebApi.Caching.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class DelayedInvalidatesCacheAttribute : Attribute
    {
        internal readonly TimeSpan InvalidatesAfter;

        public DelayedInvalidatesCacheAttribute(string invalidateAfter, params string[] cacheGroups)
        {
            InvalidatesAfter = TimeSpan.Parse(invalidateAfter);
            CacheGroups = cacheGroups;
        }
                
        public string[] CacheGroups { get; }
    }
}