using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace WorkBoard.Api.Extensions;

/// <summary>Registers tracing and metrics.</summary>
public static class ObservabilityRegistration
{
    /// <summary>Adds OpenTelemetry tracing and metrics with the OTLP exporter.</summary>
    /// <remarks>
    /// With no collector configured the exporter drops what it collects, which is why
    /// <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is unset by default rather than pointed at a placeholder:
    /// an endpoint that does not answer produces a retry and a log line per batch.
    /// </remarks>
    public static IServiceCollection AddWorkBoardObservability(this IServiceCollection services, string version)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("WorkBoard.Api", serviceVersion: version))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options =>

                    // Health probes run every few seconds and are never interesting; sampling them
                    // costs storage and drowns the traces that matter.
                    options.Filter = context => !context.Request.Path.StartsWithSegments("/health"))
                .AddHttpClientInstrumentation()

                // Database spans, so a slow request can be traced to the round trip that caused it.
                // The statement text is left at the instrumentation default: EF parameterises
                // everything, so the text carries no values, but it is still the kind of detail
                // worth opting into deliberately rather than exporting from every environment.
                .AddSqlClientInstrumentation()
                .AddOtlpExporter())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddOtlpExporter());

        return services;
    }
}
