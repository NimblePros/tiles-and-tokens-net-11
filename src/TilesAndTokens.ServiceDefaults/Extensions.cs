using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

/// <summary>Aspire service defaults: logs, metrics and traces go to the Aspire dashboard.</summary>
public static class Extensions
{
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();
        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.AddOpenTelemetry()
            .WithMetrics(metrics => metrics.AddRuntimeInstrumentation())
            .WithTracing(tracing => tracing.AddSource(builder.Environment.ApplicationName));

        // Set by the AppHost: sends logs, metrics and traces to the Aspire dashboard.
        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            builder.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }
}
