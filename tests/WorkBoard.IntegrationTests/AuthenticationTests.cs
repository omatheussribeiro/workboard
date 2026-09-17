using System.Net;
using WorkBoard.Application.DTOs.Auth;

namespace WorkBoard.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class AuthenticationTests(WorkBoardApiFactory factory)
{
    private readonly Scenario _scenario = new(factory);

    [Fact]
    public async Task Registering_returns_a_usable_token_pair()
    {
        var (client, user, refreshToken) = await _scenario.NewUserAsync();

        user.Email.Should().EndWith("@workboard.test");
        refreshToken.Should().NotBeNullOrWhiteSpace();

        var me = await client.GetAsync("/api/auth/me");
        me.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task The_same_email_cannot_be_registered_twice()
    {
        var client = new ApiClient(factory.CreateClient());
        var email = $"duplicate-{Guid.NewGuid():N}@workboard.test";
        var request = new RegisterRequest(email, "Duplicate", "integration-test-password-1");

        (await client.PostAsync("/api/auth/register", request)).StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await client.PostAsync("/api/auth/register", request);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Email_comparison_ignores_case()
    {
        var client = new ApiClient(factory.CreateClient());
        var email = $"MiXeD-{Guid.NewGuid():N}@WorkBoard.Test";

        await client.PostAsync("/api/auth/register", new RegisterRequest(email, "Mixed", "integration-test-password-1"));

        var login = await client.PostAsync<LoginRequest, AuthResponse>(
            "/api/auth/login",
            new LoginRequest(email.ToUpperInvariant(), "integration-test-password-1"));

        login.User.Email.Should().Be(email.ToLowerInvariant(), "addresses are normalised on the way in");
    }

    [Fact]
    public async Task A_wrong_password_and_an_unknown_account_fail_identically()
    {
        var client = new ApiClient(factory.CreateClient());
        var (_, user, _) = await _scenario.NewUserAsync();

        var wrongPassword = await client.PostAsync(
            "/api/auth/login",
            new LoginRequest(user.Email, "the-wrong-password-entirely"));

        var unknownAccount = await client.PostAsync(
            "/api/auth/login",
            new LoginRequest($"nobody-{Guid.NewGuid():N}@workboard.test", "integration-test-password-1"));

        // Same status and same body. A different response for a known address turns login into an
        // account-enumeration oracle.
        wrongPassword.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        unknownAccount.StatusCode.Should().Be(wrongPassword.StatusCode);

        var first = await wrongPassword.Content.ReadAsStringAsync();
        var second = await unknownAccount.Content.ReadAsStringAsync();
        ExtractDetail(first).Should().Be(ExtractDetail(second));
    }

    [Fact]
    public async Task An_unauthenticated_request_is_rejected()
    {
        var client = new ApiClient(factory.CreateClient());

        var response = await client.GetAsync("/api/workspaces");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_tampered_token_is_rejected()
    {
        var (client, _, _) = await _scenario.NewUserAsync();
        var response = await client.GetAsync("/api/workspaces");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // A structurally valid JWT whose signature will not verify.
        client.Authenticate("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhdHRhY2tlciJ9.not-a-real-signature");

        (await client.GetAsync("/api/workspaces")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refreshing_rotates_the_token()
    {
        var (client, _, refreshToken) = await _scenario.NewUserAsync();

        var refreshed = await client.PostAsync<RefreshRequest, AuthResponse>(
            "/api/auth/refresh",
            new RefreshRequest(refreshToken));

        refreshed.RefreshToken.Should().NotBe(refreshToken);
        refreshed.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// The mitigation for a stolen refresh token.
    /// </summary>
    /// <remarks>
    /// Replaying a rotated token means someone has a copy. The response is to kill the whole chain:
    /// the attacker gains nothing, and the legitimate user is logged out, which they will notice.
    /// </remarks>
    [Fact]
    public async Task Replaying_a_rotated_refresh_token_revokes_the_whole_chain()
    {
        var (client, _, original) = await _scenario.NewUserAsync();

        var rotated = await client.PostAsync<RefreshRequest, AuthResponse>(
            "/api/auth/refresh",
            new RefreshRequest(original));

        var replay = await client.PostAsync("/api/auth/refresh", new RefreshRequest(original));
        replay.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var afterReplay = await client.PostAsync("/api/auth/refresh", new RefreshRequest(rotated.RefreshToken));
        afterReplay.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "detecting reuse revokes every live token for that user, not just the replayed one");
    }

    [Fact]
    public async Task Logging_out_invalidates_the_refresh_token()
    {
        var (client, _, refreshToken) = await _scenario.NewUserAsync();

        var logout = await client.PostAsync("/api/auth/logout", new RefreshRequest(refreshToken));
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var refresh = await client.PostAsync("/api/auth/refresh", new RefreshRequest(refreshToken));
        refresh.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Logging_out_an_unknown_token_still_succeeds()
    {
        var (client, _, _) = await _scenario.NewUserAsync();

        var response = await client.PostAsync("/api/auth/logout", new RefreshRequest("a-token-that-never-existed"));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent,
            "the caller wanted the session gone and it is gone; an error would only invite probing");
    }

    [Fact]
    public async Task A_weak_password_is_rejected_with_a_problem_details_body()
    {
        var client = new ApiClient(factory.CreateClient());

        var response = await client.PostAsync(
            "/api/auth/register",
            new RegisterRequest($"weak-{Guid.NewGuid():N}@workboard.test", "Weak", "short"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Password").And.Contain("errors");
    }

    [Fact]
    public async Task Responses_carry_a_correlation_id_and_the_security_headers()
    {
        var (client, _, _) = await _scenario.NewUserAsync();

        var response = await client.GetAsync("/api/workspaces");

        response.Headers.GetValues("X-Correlation-Id").Should().ContainSingle();
        response.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
        response.Headers.GetValues("X-Frame-Options").Should().Contain("DENY");
    }

    [Fact]
    public async Task A_supplied_correlation_id_is_echoed_back()
    {
        var (client, _, _) = await _scenario.NewUserAsync();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/workspaces");
        request.Headers.Add("X-Correlation-Id", "trace-from-the-caller");

        var response = await client.Http.SendAsync(request);

        response.Headers.GetValues("X-Correlation-Id").Should().Contain("trace-from-the-caller");
    }

    [Fact]
    public async Task A_correlation_id_containing_junk_is_replaced_rather_than_echoed()
    {
        var (client, _, _) = await _scenario.NewUserAsync();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/workspaces");
        request.Headers.TryAddWithoutValidation("X-Correlation-Id", "injected\";drop-table");

        var response = await client.Http.SendAsync(request);

        response.Headers.GetValues("X-Correlation-Id").Should().NotContain(
            "injected\";drop-table",
            "the value lands in log output, so it is filtered rather than trusted");
    }

    private static string ExtractDetail(string problemJson)
    {
        using var document = System.Text.Json.JsonDocument.Parse(problemJson);
        return document.RootElement.GetProperty("detail").GetString() ?? string.Empty;
    }
}
