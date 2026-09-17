using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace WorkBoard.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class ApiDocumentationTests(WorkBoardApiFactory factory)
{
    [Theory]
    [InlineData("/scalar/v1", "text/html")]
    [InlineData("/swagger/index.html", "text/html")]
    [InlineData("/openapi/v1.json", "application/json")]
    public async Task Documentation_routes_are_public_and_available(string route, string mediaType)
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(route);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be(mediaType);
    }

    [Theory]
    [InlineData("/scalar/v1")]
    [InlineData("/swagger/index.html")]
    public async Task Documentation_pages_are_not_blocked_by_the_json_only_content_policy(string route)
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(route);

        response.Headers.Contains("Content-Security-Policy").Should().BeFalse();
    }

    [Fact]
    public async Task Api_root_redirects_to_scalar_when_documentation_is_enabled()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.OriginalString.Should().Be("/scalar/v1");
    }
}
