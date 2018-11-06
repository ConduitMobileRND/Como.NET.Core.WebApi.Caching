using Microsoft.AspNetCore.Mvc;

namespace Como.WebApi.Caching
{
    public class CacheGetResult
    {
        public CacheGetResult(bool hit, ActionResult valueFromCache)
        {
            Hit = hit;
            ValueFromCache = valueFromCache;
        }

        public bool Hit { get; }
        public ActionResult ValueFromCache { get; }
    }
}