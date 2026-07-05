using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR;

namespace Sayra.Server.Scaling;

public static class RedisScalingExtensions
{
    public static ISignalRServerBuilder AddRedisScaling(this ISignalRServerBuilder builder, string connectionString)
    {
        return builder.AddStackExchangeRedis(connectionString, options =>
        {
            options.Configuration.ChannelPrefix = "SayraServer";
        });
    }
}
