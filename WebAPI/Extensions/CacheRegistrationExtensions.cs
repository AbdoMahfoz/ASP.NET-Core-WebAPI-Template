using System.IO;

using BusinessLogic.Implementations.Cache;

using BusinessLogic.Interfaces;

using Microsoft.Extensions.DependencyInjection;

using Models.Helpers;

using StackExchange.Redis;


namespace WebAPI.Extensions;


/// <summary>
///     Chooses which cache the application runs on from configuration: AppSettings.Redis.Use selects the shared Redis backplane, otherwise the in-process cache is used. The multiplexer is registered as a factory so nothing dials a server until something actually uses the cache, which keeps the Redis wiring resolvable in tests and in a process that has not been given a server yet. The cache also needs an IHttpContextAccessor because a refresh runs in its own DI scope and has to recover the tenant from the request that triggered it.
/// </summary>
public static class CacheRegistrationExtensions

{


    public static IServiceCollection AddConfiguredCache(this IServiceCollection services, AppSettings appSettings)

    {

        services.AddHttpContextAccessor();


        var redis = appSettings?.Redis;


        if (redis != null && redis.Use)

        {

            services.AddSingleton<IConnectionMultiplexer>(provider => ConnectToRedis(redis));


            services.AddSingleton<ICache, RedisCache>();


            return services;

        }


        services.AddSingleton<ICache, MemoryCache>();


        return services;

    }


    private static ConnectionMultiplexer ConnectToRedis(AppSettings.RedisObject redis)

    {

        var options = ConfigurationOptions.Parse(redis.ConnectionString);


        // A cache backplane must not take the process down while the server is briefly unreachable: connect lazily and retry.
        options.AbortOnConnectFail = false;


        // ValidateCertificate maps to the certificate-strictness knob StackExchange.Redis 3.x exposes (revocation checking); the 2.x custom validation callback hook no longer exists on ConfigurationOptions.
        options.CheckCertificateRevocation = redis.ValidateCertificate;


        return ConnectionMultiplexer.Connect(options, TextWriter.Null);

    }

}
