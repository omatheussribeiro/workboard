using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WorkBoard.IntegrationTests;

/// <summary>
/// A thin wrapper over <see cref="HttpClient"/> that speaks this API.
/// </summary>
/// <remarks>
/// Exists so the tests read as scenarios rather than as HTTP plumbing. It deliberately does not
/// swallow failures: every call returns the response so a test can assert on the status code, which
/// is often the thing being tested.
/// </remarks>
public sealed class ApiClient(HttpClient http)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public HttpClient Http { get; } = http;

    public void Authenticate(string accessToken) =>
        Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    public void SignOut() => Http.DefaultRequestHeaders.Authorization = null;

    public Task<HttpResponseMessage> PostAsync<T>(string url, T body) =>
        Http.PostAsJsonAsync(url, body, Json);

    public Task<HttpResponseMessage> PutAsync<T>(string url, T body) =>
        Http.PutAsJsonAsync(url, body, Json);

    public Task<HttpResponseMessage> GetAsync(string url) => Http.GetAsync(new Uri(url, UriKind.Relative));

    public Task<HttpResponseMessage> DeleteAsync(string url) => Http.DeleteAsync(new Uri(url, UriKind.Relative));

    public static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            // The body of a failure is a ProblemDetails, and it usually says exactly what went wrong.
            // Surfacing it here turns "expected OK but got 409" into a message worth reading.
            throw new InvalidOperationException($"{(int)response.StatusCode} from {response.RequestMessage?.RequestUri}: {body}");
        }

        return JsonSerializer.Deserialize<T>(body, Json)
               ?? throw new InvalidOperationException($"Could not deserialise a {typeof(T).Name} from: {body}");
    }

    public async Task<T> GetAsync<T>(string url) => await ReadAsync<T>(await GetAsync(url));

    public async Task<T> PostAsync<TBody, T>(string url, TBody body) => await ReadAsync<T>(await PostAsync(url, body));

    public async Task<T> PutAsync<TBody, T>(string url, TBody body) => await ReadAsync<T>(await PutAsync(url, body));
}
