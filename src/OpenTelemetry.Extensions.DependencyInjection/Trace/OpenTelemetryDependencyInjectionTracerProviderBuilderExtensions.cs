// <copyright file="OpenTelemetryDependencyInjectionTracerProviderBuilderExtensions.cs" company="OpenTelemetry Authors">
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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTelemetry.Internal;

namespace OpenTelemetry.Trace;

/// <summary>
/// Contains extension methods for the <see cref="TracerProviderBuilder"/> class.
/// </summary>
public static class OpenTelemetryDependencyInjectionTracerProviderBuilderExtensions
{
    /// <summary>
    /// Adds instrumentation to the provider.
    /// </summary>
    /// <remarks>
    /// Note: The type specified by <typeparamref name="T"/> will be
    /// registered as a singleton service into application services.
    /// </remarks>
    /// <typeparam name="T">Instrumentation type.</typeparam>
    /// <param name="tracerProviderBuilder"><see cref="TracerProviderBuilder"/>.</param>
    /// <returns>The supplied <see cref="TracerProviderBuilder"/> for chaining.</returns>
    public static TracerProviderBuilder AddInstrumentation<T>(this TracerProviderBuilder tracerProviderBuilder)
        where T : class
    {
        tracerProviderBuilder.ConfigureServices(services => services.TryAddSingleton<T>());

        tracerProviderBuilder.ConfigureBuilder((sp, builder) =>
        {
            builder.AddInstrumentation(() => sp.GetRequiredService<T>());
        });

        return tracerProviderBuilder;
    }

    /// <summary>
    /// Adds instrumentation to the provider.
    /// </summary>
    /// <typeparam name="T">Instrumentation type.</typeparam>
    /// <param name="tracerProviderBuilder"><see cref="TracerProviderBuilder"/>.</param>
    /// <param name="instrumentation">Instrumentation instance.</param>
    /// <returns>The supplied <see cref="TracerProviderBuilder"/> for chaining.</returns>
    public static TracerProviderBuilder AddInstrumentation<T>(this TracerProviderBuilder tracerProviderBuilder, T instrumentation)
        where T : class
    {
        Guard.ThrowIfNull(instrumentation);

        tracerProviderBuilder.ConfigureBuilder((sp, builder) =>
        {
            builder.AddInstrumentation(() => instrumentation);
        });

        return tracerProviderBuilder;
    }

    /// <summary>
    /// Adds instrumentation to the provider.
    /// </summary>
    /// <typeparam name="T">Instrumentation type.</typeparam>
    /// <param name="tracerProviderBuilder"><see cref="TracerProviderBuilder"/>.</param>
    /// <param name="instrumentationFactory">Instrumentation factory.</param>
    /// <returns>The supplied <see cref="TracerProviderBuilder"/> for chaining.</returns>
    public static TracerProviderBuilder AddInstrumentation<T>(
        this TracerProviderBuilder tracerProviderBuilder,
        Func<IServiceProvider, T> instrumentationFactory)
        where T : class
    {
        Guard.ThrowIfNull(instrumentationFactory);

        tracerProviderBuilder.ConfigureBuilder((sp, builder) =>
        {
            builder.AddInstrumentation(() => instrumentationFactory(sp));
        });

        return tracerProviderBuilder;
    }

    /// <summary>
    /// Adds instrumentation to the provider.
    /// </summary>
    /// <typeparam name="T">Instrumentation type.</typeparam>
    /// <param name="tracerProviderBuilder"><see cref="TracerProviderBuilder"/>.</param>
    /// <param name="instrumentationFactory">Instrumentation factory.</param>
    /// <returns>The supplied <see cref="TracerProviderBuilder"/> for chaining.</returns>
    public static TracerProviderBuilder AddInstrumentation<T>(
        this TracerProviderBuilder tracerProviderBuilder,
        Func<IServiceProvider, TracerProvider, T> instrumentationFactory)
        where T : class
    {
        Guard.ThrowIfNull(instrumentationFactory);

        tracerProviderBuilder.ConfigureBuilder((sp, builder) =>
        {
            if (tracerProviderBuilder is ITracerProviderBuilder iTracerProviderBuilder
                && iTracerProviderBuilder.Provider != null)
            {
                builder.AddInstrumentation(() => instrumentationFactory(sp, iTracerProviderBuilder.Provider));
            }
        });

        return tracerProviderBuilder;
    }

    /// <summary>
    /// Register a callback action to configure the <see
    /// cref="IServiceCollection"/> where tracing services are configured.
    /// </summary>
    /// <remarks>
    /// Note: Tracing services are only available during the application
    /// configuration phase.
    /// </remarks>
    /// <param name="tracerProviderBuilder"><see cref="TracerProviderBuilder"/>.</param>
    /// <param name="configure">Configuration callback.</param>
    /// <returns>The supplied <see cref="TracerProviderBuilder"/> for chaining.</returns>
    public static TracerProviderBuilder ConfigureServices(
        this TracerProviderBuilder tracerProviderBuilder,
        Action<IServiceCollection> configure)
    {
        if (tracerProviderBuilder is ITracerProviderBuilder iTracerProviderBuilder)
        {
            iTracerProviderBuilder.ConfigureServices(configure);
        }

        return tracerProviderBuilder;
    }

    /// <summary>
    /// Register a callback action to configure the <see
    /// cref="TracerProviderBuilder"/> once the application <see
    /// cref="IServiceProvider"/> is available.
    /// </summary>
    /// <param name="tracerProviderBuilder"><see cref="TracerProviderBuilder"/>.</param>
    /// <param name="configure">Configuration callback.</param>
    /// <returns>The supplied <see cref="TracerProviderBuilder"/> for chaining.</returns>
    public static TracerProviderBuilder ConfigureBuilder(
        this TracerProviderBuilder tracerProviderBuilder,
        Action<IServiceProvider, TracerProviderBuilder> configure)
    {
        if (tracerProviderBuilder is IDeferredTracerProviderBuilder deferredTracerProviderBuilder)
        {
            deferredTracerProviderBuilder.Configure(configure);
        }

        return tracerProviderBuilder;
    }
}
