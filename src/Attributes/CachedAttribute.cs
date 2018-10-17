using System;

namespace Como.WebApi.Caching.Attributes
{
    public class CachedAttribute : Attribute
    {
        internal TimeSpan? ExpireAfterTimeSpan;

        public CachedAttribute(params string[] cacheGroups)
        {
            if (cacheGroups == null || cacheGroups.Length == 0)
            {
                throw new ArgumentException("At least one cache group should be specified", nameof(cacheGroups));
            }

            CacheGroups = cacheGroups;
        }

        public string[] CacheGroups { get; }
        public string ScopeName { get; set; }

        public string ExpireAfter
        {
            set => ExpireAfterTimeSpan = value == null ? (TimeSpan?) null : TimeSpan.Parse(value);
            get => ExpireAfterTimeSpan.ToString();
        }

        public bool SlidingExpiration { get; set; }
    }
}