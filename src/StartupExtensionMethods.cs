using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Como.WebApi.Caching
{
    public static class StartupExtensionMethods
    {

        public static IMvcBuilder AddWebApiCaching<TParamsResolver>(this IMvcBuilder builder)
            where TParamsResolver : class, ICacheParametersResolver
        {
            return builder.AddWebApiCaching<TParamsResolver, RedisWebApiWebApiCacheAdapter>();
        }

        public static IMvcBuilder AddWebApiCaching<TParamsResolver, TWebApiCacheAdapter>(this IMvcBuilder builder)
            where TParamsResolver : class, ICacheParametersResolver
            where TWebApiCacheAdapter : class, IWebApiCacheAdapter
        {
            builder.Services.AddSingleton<SerializationHelper>();
            builder.Services.AddSingleton<IWebApiCacheAdapter, TWebApiCacheAdapter>();
            builder.Services.AddSingleton<DelayedInvalidationQueue>();
            builder.Services.AddTransient<IHostedService, DelayedInvalidationQueueProcessor>();
            builder.Services.AddSingleton<ICacheParametersResolver, TParamsResolver>();
            builder.AddMvcOptions(options => options.Filters.Add<CachingFilter>());
            return builder;
        }
    }
}