using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;

namespace WorkBoard.Api.Extensions;

/// <summary>
/// Rate-limit policies, partitioned by caller.
/// </summary>
/// <remarks>
/// <para>
/// Three policies rather than one global limit, because the endpoints differ in what abusing them
/// costs. <c>auth</c> is strict and keyed by IP: it is anonymous, and each call runs BCrypt at cost
/// 12, so a few hundred requests a minute is a CPU exhaustion attack as much as a credential-stuffing
/// one. <c>search</c> is keyed by user and sized for a type-ahead. The global default is generous and
/// exists to stop a runaway client, not to shape traffic.
/// </para>
/// <para>
/// Authenticated policies partition on the user id, not the IP. Partitioning on IP would make a whole
/// office behind one NAT share a bucket, so one heavy user would throttle their colleagues.
/// </para>
/// <para>
/// This is in-process, so each replica has its own counters and the effective limit multiplies by
/// replica count. That is a known limitation of <c>System.Threading.RateLimiting</c>, acceptable here
/// and not acceptable for a hard quota — that needs shared state (Redis) or an API gateway.
/// </para>
/// </remarks>
public static class RateLimitPolicies
{
    public const string Auth = "auth";

    public const string Search = "search";

    public static IServiceCollection AddWorkBoardRateLimiting(this IServiceCollection services)
    {
        // BindConfiguration, not configuration.Get<T>() at this line. The difference matters: reading
        // eagerly captures whatever sources exist at composition time, so a source added afterwards —
        // a test host, a provider registered later — is silently ignored and the running limits are
        // not the configured ones. Binding defers the read until the options are first resolved.
        services.AddOptions<RateLimitOptions>().BindConfiguration(RateLimitOptions.SectionName);

        return services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Tells a client when to come back instead of leaving it to guess and retry immediately.
            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }

                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new { title = "Too many requests", status = 429 },
                    cancellationToken);
            };

            options.AddPolicy(Auth, context => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: ClientIp(context),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = Limits(context).AuthPerMinute,
                    Window = TimeSpan.FromMinutes(1),

                    // No queue. A caller over the login limit should be told so immediately, not held
                    // on a connection — queueing here would turn the limiter into the bottleneck.
                    QueueLimit = 0,
                }));

            options.AddPolicy(Search, context => RateLimitPartition.GetTokenBucketLimiter(
                partitionKey: UserOrIp(context),
                factory: _ =>
                {
                    var limits = Limits(context);

                    return new TokenBucketRateLimiterOptions
                    {
                        // A token bucket rather than a fixed window: typing produces bursts, and a
                        // bucket absorbs a burst while still capping the sustained rate.
                        TokenLimit = limits.SearchBurst,
                        TokensPerPeriod = limits.SearchPerTenSeconds,
                        ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                        QueueLimit = 0,
                    };
                }));

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: UserOrIp(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = Limits(context).GlobalPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));
        });
    }

    private static RateLimitOptions Limits(HttpContext context) =>
        context.RequestServices.GetRequiredService<IOptions<RateLimitOptions>>().Value;

    private static string ClientIp(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static string UserOrIp(HttpContext context) =>
        context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? ClientIp(context);
}
