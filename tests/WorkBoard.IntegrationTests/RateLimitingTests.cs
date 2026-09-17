using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkBoard.Application.DTOs.Auth;

namespace WorkBoard.IntegrationTests;

/// <summary>
/// Proves the limiter actually rejects, with limits low enough to reach in a test.
/// </summary>
/// <remarks>
/// The shared factory raises every limit out of the way so the rest of the suite can run. This class
/// takes that same factory and lowers the auth limit for its own host, which is the only way to
/// assert the behaviour without making every other test fragile.
/// </remarks>
[Collection(ApiCollection.Name)]
public sealed class RateLimitingTests(WorkBoardApiFactory factory)
{
    [Fact]
    public async Task Repeated_login_attempts_are_rejected_with_429_and_a_Retry_After()
    {
        const int limit = 3;

        using var constrained = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["RateLimiting:AuthPerMinute"] = limit.ToString(System.Globalization.CultureInfo.InvariantCulture),
                })));

        var client = new ApiClient(constrained.CreateClient());
        var request = new LoginRequest($"nobody-{Guid.NewGuid():N}@workboard.test", "whatever-it-is-wrong");

        var statuses = new List<HttpStatusCode>();
        for (var attempt = 0; attempt < limit + 2; attempt++)
        {
            var response = await client.PostAsync("/api/auth/login", request);
            statuses.Add(response.StatusCode);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                response.Headers.RetryAfter.Should().NotBeNull(
                    "a client that is not told when to come back will simply retry immediately");
            }
        }

        statuses.Should().Contain(HttpStatusCode.TooManyRequests);
        statuses.Take(limit).Should().AllBeEquivalentTo(HttpStatusCode.Forbidden,
            "the attempts within the limit are answered normally, as failed logins");
    }

    [Fact]
    public void The_limits_come_from_configuration_rather_than_being_hard_coded()
    {
        using var scope = factory.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        configuration.GetSection(WorkBoard.Api.Extensions.RateLimitOptions.SectionName)
            .Get<WorkBoard.Api.Extensions.RateLimitOptions>()
            .Should().NotBeNull();
    }
}
