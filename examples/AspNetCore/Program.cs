// <copyright file="Program.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System.Reflection;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var appBuilder = WebApplication.CreateBuilder(args);

// Note: Switch between Zipkin/Jaeger/OTLP/Console by setting UseTracingExporter in appsettings.json.
var tracingExporter = appBuilder.Configuration.GetValue<string>("UseTracingExporter").ToLowerInvariant();

// Note: Switch between Prometheus/OTLP/Console by setting UseMetricsExporter in appsettings.json.
var metricsExporter = appBuilder.Configuration.GetValue<string>("UseMetricsExporter").ToLowerInvariant();

// Note: Switch between Console/OTLP by setting UseLogExporter in appsettings.json.
var logExporter = appBuilder.Configuration.GetValue<string>("UseLogExporter").ToLowerInvariant();

// Build a resource configuration action to set service information.
Action<ResourceBuilder> configureResource = r => r.AddService(
    serviceName: appBuilder.Configuration.GetValue<string>("ServiceName"),
    serviceVersion: Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown",
    serviceInstanceId: Environment.MachineName);

// Configure OpenTelemetry tracing & metrics with auto-start using the
// StartWithHost extension from OpenTelemetry.Extensions.Hosting.
appBuilder.Services.AddOpenTelemetry()
    .ConfigureResource(configureResource)
    .WithTracing(builder =>
    {
        // Tracing

        builder
            .SetSampler(new AlwaysOnSampler())
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation();

        // Use IConfiguration binding for AspNetCore instrumentation options.
        appBuilder.Services.Configure<AspNetCoreInstrumentationOptions>(appBuilder.Configuration.GetSection("AspNetCoreInstrumentation"));

        switch (tracingExporter)
        {
            case "jaeger":
                builder.AddJaegerExporter();

                builder.ConfigureServices(services =>
                {
                    // Use IConfiguration binding for Jaeger exporter options.
                    services.Configure<JaegerExporterOptions>(appBuilder.Configuration.GetSection("Jaeger"));

                    // Customize the HttpClient that will be used when JaegerExporter is configured for HTTP transport.
                    services.AddHttpClient("JaegerExporter", configureClient: (client) => client.DefaultRequestHeaders.Add("X-MyCustomHeader", "value"));
                });
                break;

            case "zipkin":
                builder.AddZipkinExporter();

                builder.ConfigureServices(services =>
                {
                    // Use IConfiguration binding for Zipkin exporter options.
                    services.Configure<ZipkinExporterOptions>(appBuilder.Configuration.GetSection("Zipkin"));
                });
                break;

            case "otlp":
                builder.AddOtlpExporter(otlpOptions =>
                {
                    // Use IConfiguration directly for Otlp exporter endpoint option.
                    otlpOptions.Endpoint = new Uri(appBuilder.Configuration.GetValue<string>("Otlp:Endpoint"));
                });
                break;

            default:
                builder.AddConsoleExporter();
                break;
        }
    })
    .WithMetrics(builder =>
    {
        // Metrics

        builder
            .AddRuntimeInstrumentation()
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation();

        switch (metricsExporter)
        {
            case "prometheus":
                builder.AddPrometheusExporter();
                break;
            case "otlp":
                builder.AddOtlpExporter(otlpOptions =>
                {
                    // Use IConfiguration directly for Otlp exporter endpoint option.
                    otlpOptions.Endpoint = new Uri(appBuilder.Configuration.GetValue<string>("Otlp:Endpoint"));
                });
                break;
            default:
                builder.AddConsoleExporter();
                break;
        }
    })
    .StartWithHost();

// Clear default logging providers used by WebApplication host.
appBuilder.Logging.ClearProviders();

// Configure OpenTelemetry Logging.
appBuilder.Logging.AddOpenTelemetry(options =>
{
    // Note: See appsettings.json Logging:OpenTelemetry section for configuration.

    options.ConfigureResource(configureResource);

    switch (logExporter)
    {
        case "otlp":
            options.AddOtlpExporter(otlpOptions =>
            {
                // Use IConfiguration directly for Otlp exporter endpoint option.
                otlpOptions.Endpoint = new Uri(appBuilder.Configuration.GetValue<string>("Otlp:Endpoint"));
            });
            break;
        default:
            options.AddConsoleExporter();
            break;
    }
});

appBuilder.Services.AddControllers();

appBuilder.Services.AddEndpointsApiExplorer();

appBuilder.Services.AddSwaggerGen();

var app = appBuilder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Configure OpenTelemetry Prometheus AspNetCore middleware scrape endpoint if enabled.
if (metricsExporter.Equals("prometheus", StringComparison.OrdinalIgnoreCase))
{
    app.UseOpenTelemetryPrometheusScrapingEndpoint();
}

app.Run();
