# .NET-Core-Web.Api.Caching
A library for .NET Core 2 WebApi methods caching 

[![NuGet Pre Release](https://img.shields.io/nuget/vpre/Como.NetCore.WebApi.Caching.svg?style=popout)](https://www.nuget.org/packages/Como.NetCore.WebApi.Caching/)

* Setup:
  * startup.cs
    ```csharp
    ConfigureServices(IServiceCollection services)  {
        ...
        services.AddSingleton<IWebApiCacheAdapter, RedisWebApiWebApiCacheAdapter>();
        services.AddSingleton<ICacheParametersResolver, MyCacheParametersResolver>();
        ...
        services.AddMvc(options => {
            ...
            options.Filters.Add<MyAuthenticationFilter>();
            options.Filters.Add<MyAuthrizationFilter>();
            // note to call this method only after the authorization and authentication filters (if any) were registered,
            // otherwise, non-authenticated/non-authorized users might get sensitive data from cache since they skipped the required authorization and/or authentication processes.
            options.UseWebApiCaching();
        });
    }
    ```
  * MyCacheParametersResolver.cs
      ```csharp
        public class MyCacheParametersResolver : ICacheParametersResolver
        {
            private readonly ISomeDependencyInectedInterface _somethingNeeded;
            public MyCacheParametersResolver(ISomeDependencyInectedInterface somethingNeeded)
            {
                _somethingNeeded = somethingNeeded;
            }
    
            public string ResolveScopeValue(string scopeName, ActionExecutingContext context)
            {
                switch (scopeName)
                {
                    case (MyCacheConstants.CacheScopePerUser):
                        return _somethingNeeded.ResolveUserId(context) ?? string.Empty;
                    default:
                        return string.Empty;
                }
            }
            
            // default expiration of an item if attribute does not specify another value:
            public TimeSpan? DefaultExpiration => TimeSpan.FromDays(1); 
    
            // return the controllers in your WebApi assembly
            public ICollection<Type> GetWebApiControllers()
            {
                var controllerBaseType = typeof(Controller);
                return typeof(MyCacheParametersResolver).Assembly.GetTypes()
                    .Where(t => controllerBaseType.IsAssignableFrom(t)).ToList();
            }
        }
        ``` 
* Usage
    * MySomethingController.cs
        ```csharp
        using Como.WebApi.Caching.Attributes;
        ...
        public class MySomethingController : Controller
        {
            ...
            [HttpGet("{id}")]
            [Cached(MyCacheConstants.CacheGroupSomething, ScopeName = MyCacheConstants.CacheScopePerUser)]
            // this method will be cached until another method invalidates the MyCacheConstants.CacheGroupSomething group
            public IActionResult GetSomething(string id) { ... }
            
            [HttpPost]
            [InvalidatesCache(MyCacheConstants.CacheGroupSomething, MyCacheConstants.CacheGroupAnotherThing)]
            // this method will invalidate all other methods in the MyCacheConstants.CacheGroupSomething and MyCacheConstants.CacheGroupAnotherThing groups
            public IActionResult SaveSomething([FromBody] Something what) { ... }
            
            [HttpGet()]
            [Cached(MyCacheConstants.CacheGroupSomething, 
            ScopeName = MyCacheConstants.CacheScopePerUser,
            ExpireAfter = "00:00:30", 
            SlidingExpiration = true)]
            // this method will be cached until another method invalidates the MyCacheConstants.CacheGroupSomething group or until the method haven't been called for 30 seconds straight
            public IActionResult GetSomethings() { ... }
            
            [HttpGet()]
            [Cached(MyCacheConstants.CacheGroupSomething, 
            ScopeName = MyCacheConstants.CacheScopePerUser,
            ExpireAfter = "00:00:30", 
            SlidingExpiration = false)]
            // this method will be cached until another method invalidates the MyCacheConstants.CacheGroupSomething group or after 30 seconds since cached
            public IActionResult GetSomethings() { ... }
        }
        ```
