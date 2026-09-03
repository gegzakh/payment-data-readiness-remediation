using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using PDR.BuildingBlocks.Core.Correlation;
using Serilog;
using Serilog.Events;

namespace PDR.BuildingBlocks.Observability;

public static class ObservabilityExtensions
{
    /// <summary>
    /// Structured logging (Serilog → console JSON + Seq) and OpenTelemetry traces/metrics, with the same
    /// enrichment in every service so logs correlate across service boundaries (NFR-007).
    /// </summary>
    public static IHostApplicationBuilder AddPdrObservability(this IHostApplicationBuilder builder, string serviceName)
    {
        var seqUrl = builder.Configuration["Observability:SeqUrl"];
        var otlpEndpoint = builder.Configuration["Observability:OtlpEndpoint"];

        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("service", serviceName)
            .Enrich.WithProperty("environment", builder.Environment.EnvironmentName)
            .WriteTo.Console(new Serilog.Formatting.Compact.CompactJsonFormatter());

        if (!string.IsNullOrWhiteSpace(seqUrl))
        {
            loggerConfiguration.WriteTo.Seq(seqUrl);
        }

        Log.Logger = loggerConfiguration.CreateLogger();
        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(Log.Logger);

        builder.Services.AddSingleton<ICorrelationContext, CorrelationContext>();

        builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(o => o.RecordException = true)
                    .AddHttpClientInstrumentation();

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(exporter => exporter.Endpoint = new Uri(otlpEndpoint));
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    metrics.AddOtlpExporter(exporter => exporter.Endpoint = new Uri(otlpEndpoint));
                }
            });

        return builder;
    }

    public static IApplicationBuilder UsePdrCorrelation(this IApplicationBuilder app) =>
        app.UseMiddleware<CorrelationMiddleware>();
}
