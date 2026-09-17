using System.Text.Json.Serialization;

namespace WorkBoard.Api.Extensions;

/// <summary>Configures how the API serialises its responses.</summary>
public static class JsonRegistration
{
    /// <summary>Pins the two serialisation choices that are part of the API contract.</summary>
    /// <remarks>
    /// Enums travel as their names. Numbers make the payload unreadable and turn reordering an enum
    /// into a silent breaking change for every client — the values shift, the wire format does not,
    /// and nothing fails until someone notices the wrong label on a card.
    /// </remarks>
    public static IServiceCollection AddWorkBoardJson(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

        return services;
    }
}
