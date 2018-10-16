using Microsoft.AspNetCore.Mvc;

namespace Como.WebApi.Caching
{
    public static class MvcExtensionMethods
    {
        public static void UseWebApiCaching(this MvcOptions options)
        {
            options.Filters.Add<CachingFilter>();
        }
    }
}