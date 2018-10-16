using Como.WebApi.Caching;

namespace Como.WebApi.Caching
{
    public class CacheGetResult
    {
        public CacheGetResult(bool hit, RawActionResult valueFromCache)
        {
            Hit = hit;
            ValueFromCache = valueFromCache;
        }

        public bool Hit { get; }
        public RawActionResult ValueFromCache { get; }
    }
}