# OTLP Exporter for OpenTelemetry .NET

[![NuGet](https://img.shields.io/nuget/v/OpenTelemetry.Exporter.OpenTelemetryProtocol.svg)](https://www.nuget.org/packages/OpenTelemetry.Exporter.OpenTelemetryProtocol)
[![NuGet](https://img.shields.io/nuget/dt/OpenTelemetry.Exporter.OpenTelemetryProtocol.svg)](https://www.nuget.org/packages/OpenTelemetry.Exporter.OpenTelemetryProtocol)

[The OTLP (OpenTelemetry Protocol) exporter](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/protocol/exporter.md)
implementation.

## Prerequisite

* [Get OpenTelemetry Collector](https://opentelemetry.io/docs/collector/)

## Installation

```shell
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
```

## Configuration

You can configure the `OtlpExporter` through `Options` types properties
and environment variables.
The `Options` type setters take precedence over the environment variables.

## Options Properties

* `BatchExportProcessorOptions`: Configuration options for the batch exporter.
  Only used if ExportProcessorType is set to Batch.

* `Endpoint`: Target to which the exporter is going to send traces or metrics.
  The endpoint must be a valid Uri with scheme (http or https) and host, and MAY
  contain a port and path.

* `ExportProcessorType`: Whether the exporter should use [Batch or Simple
  exporting
  processor](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/sdk.md#built-in-span-processors).

* `Headers`: Optional headers for the connection.

* `HttpClientFactory`: A factory function called to create the `HttpClient`
  instance that will be used at runtime to transmit telemetry over HTTP when the
  `HttpProtobuf` protocol is configured. See [Configure
  HttpClient](#configure-httpclient) for more details.

* `TimeoutMilliseconds` : Max waiting time for the backend to process a batch.

* `Protocol`: OTLP transport protocol. Supported values:
  `OtlpExportProtocol.Grpc` and `OtlpExportProtocol.HttpProtobuf`.

See the [`TestOtlpExporter.cs`](../../examples/Console/TestOtlpExporter.cs) for
an example of how to use the exporter.

## Environment Variables

The following environment variables can be used to override the default
values of the `OtlpExporterOptions`
(following the [OpenTelemetry specification](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/protocol/exporter.md)).

| Environment variable          | `OtlpExporterOptions` property        |
| ------------------------------| --------------------------------------|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | `Endpoint`                            |
| `OTEL_EXPORTER_OTLP_HEADERS`  | `Headers`                             |
| `OTEL_EXPORTER_OTLP_TIMEOUT`  | `TimeoutMilliseconds`                 |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | `Protocol` (`grpc` or `http/protobuf`)|

The following environment variables can be used to override the default
values of the `PeriodicExportingMetricReaderOptions`
(following the [OpenTelemetry specification](https://github.com/open-telemetry/opentelemetry-specification/blob/v1.12.0/specification/sdk-environment-variables.md#periodic-exporting-metricreader).

| Environment variable          | `PeriodicExportingMetricReaderOptions` property |
| ------------------------------| ------------------------------------------------|
| `OTEL_METRIC_EXPORT_INTERVAL` | `ExportIntervalMilliseconds`                    |
| `OTEL_METRIC_EXPORT_TIMEOUT`  | `ExportTimeoutMilliseconds`                     |

The following environment variables can be used to override the default
values of the attribute limits
(following the [OpenTelemetry specification](https://github.com/open-telemetry/opentelemetry-specification/blob/v1.15.0/specification/sdk-environment-variables.md#attribute-limits)).

* `OTEL_ATTRIBUTE_VALUE_LENGTH_LIMIT`
* `OTEL_ATTRIBUTE_COUNT_LIMIT`

The following environment variables can be used to override the default
values of the span limits
(following the [OpenTelemetry specification](https://github.com/open-telemetry/opentelemetry-specification/blob/v1.15.0/specification/sdk-environment-variables.md#span-limits)).

* `OTEL_SPAN_ATTRIBUTE_VALUE_LENGTH_LIMIT`
* `OTEL_SPAN_ATTRIBUTE_COUNT_LIMIT`
* `OTEL_SPAN_EVENT_COUNT_LIMIT`
* `OTEL_SPAN_LINK_COUNT_LIMIT`
* `OTEL_EVENT_ATTRIBUTE_COUNT_LIMIT`
* `OTEL_LINK_ATTRIBUTE_COUNT_LIMIT`

`FormatException` is thrown in case of an invalid value for any of the
supported environment variables.

## OTLP Logs

This package currently only supports exporting traces and metrics. Support for
exporting logs is provided by installing the
[`OpenTelemetry.Exporter.OpenTelemetryProtocol.Logs`](../OpenTelemetry.Exporter.OpenTelemetryProtocol.Logs/README.md)
package.

Once the OTLP log exporter is stable, it'll be folded into this package. Check
[this](https://github.com/open-telemetry/opentelemetry-dotnet/milestone/35)
milestone for tracking.

## Special case when using insecure channel

If your application is targeting .NET Core 3.1, and you are using an insecure
(HTTP) endpoint, the following switch must be set before adding `OtlpExporter`.

```csharp
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport",
 true);
```

See
[this](https://docs.microsoft.com/aspnet/core/grpc/troubleshoot#call-insecure-grpc-services-with-net-core-client)
for more information.

## Configure HttpClient

The `HttpClientFactory` option is provided on `OtlpExporterOptions` for users
who want to configure the `HttpClient` used by the `OtlpTraceExporter` and/or
`OtlpMetricExporter` when `HttpProtobuf` protocol is used. Simply replace the
function with your own implementation if you want to customize the generated
`HttpClient`:

```csharp
services.AddOpenTelemetry()
    .WithTracing(builder => builder
        .AddOtlpExporter(o =>
        {
            o.Protocol = OtlpExportProtocol.HttpProtobuf;
            o.HttpClientFactory = () =>
            {
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("X-MyCustomHeader", "value");
                return client;
            };
        }))
    .StartWithHost();
```

For users using
[IHttpClientFactory](https://docs.microsoft.com/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests)
you may also customize the named "OtlpTraceExporter" and/or "OtlpMetricExporter"
`HttpClient` using the built-in `AddHttpClient` extension:

```csharp
services.AddHttpClient(
    "OtlpTraceExporter",
    configureClient: (client) =>
        client.DefaultRequestHeaders.Add("X-MyCustomHeader", "value"));
```

Note: The single instance returned by `HttpClientFactory` is reused by all
export requests.

## Troubleshooting

This component uses an
[EventSource](https://docs.microsoft.com/dotnet/api/system.diagnostics.tracing.eventsource)
with the name "OpenTelemetry-Exporter-OpenTelemetryProtocol" for its internal
logging. Please refer to [SDK
troubleshooting](../OpenTelemetry/README.md#troubleshooting) for instructions on
seeing these internal logs.

## References

* [OpenTelemetry
  Collector](https://github.com/open-telemetry/opentelemetry-collector)
* [OpenTelemetry Project](https://opentelemetry.io/)
* [OTLP proto files](https://github.com/open-telemetry/opentelemetry-proto)
