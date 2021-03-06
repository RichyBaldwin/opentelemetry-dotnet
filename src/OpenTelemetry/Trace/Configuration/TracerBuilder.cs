﻿// <copyright file="TracerBuilder.cs" company="OpenTelemetry Authors">
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
using System;
using System.Collections.Generic;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Resources;

namespace OpenTelemetry.Trace.Configuration
{
    /// <summary>
    /// Build Tracers.
    /// </summary>
    public class TracerBuilder
    {
        internal TracerBuilder()
        {
        }

        internal TracerConfiguration TracerConfigurationOptions { get; private set; }

        internal Sampler Sampler { get; private set; }

        internal Resource Resource { get; private set; } = Resource.Empty;

        internal List<SpanProcessorPipelineBuilder> ProcessingPipelines { get; private set; }

        internal List<AdapterFactory> AdapterFactories { get; private set; }

        /// <summary>
        /// Configures sampler.
        /// </summary>
        /// <param name="sampler">Sampler instance.</param>
        /// <returns>Returns <see cref="TracerBuilder"/> for chaining.</returns>
        public TracerBuilder SetSampler(Sampler sampler)
        {
            this.Sampler = sampler ?? throw new ArgumentNullException(nameof(sampler));
            return this;
        }

        /// <summary>
        /// Sets the <see cref="Resource"/> describing the app associated with all traces. Overwrites currently set resource.
        /// </summary>
        /// <param name="resource">Resource to be associate with all traces.</param>
        /// <returns>Returns <see cref="TracerBuilder"/> for chaining.</returns>
        public TracerBuilder SetResource(Resource resource)
        {
            this.Resource = resource ?? Resource.Empty;
            return this;
        }

        /// <summary>
        /// Adds processing and exporting pipeline. Pipelines are executed sequentially in the order they are added.
        /// </summary>
        /// <param name="configure">Function that configures pipeline.</param>
        /// <returns>Returns <see cref="TracerBuilder"/> for chaining.</returns>
        public TracerBuilder AddProcessorPipeline(Action<SpanProcessorPipelineBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            if (this.ProcessingPipelines == null)
            {
                this.ProcessingPipelines = new List<SpanProcessorPipelineBuilder>();
            }

            var pipelineBuilder = new SpanProcessorPipelineBuilder();
            configure(pipelineBuilder);
            this.ProcessingPipelines.Add(pipelineBuilder);
            return this;
        }

        /// <summary>
        /// Adds auto-adapters for spans.
        /// </summary>
        /// <typeparam name="TAdapter">Type of adapter class.</typeparam>
        /// <param name="adapterFactory">Function that builds adapter from <see cref="Tracer"/>.</param>
        /// <returns>Returns <see cref="TracerBuilder"/> for chaining.</returns>
        public TracerBuilder AddAdapter<TAdapter>(
            Func<Tracer, TAdapter> adapterFactory)
            where TAdapter : class
        {
            if (adapterFactory == null)
            {
                throw new ArgumentNullException(nameof(adapterFactory));
            }

            if (this.AdapterFactories == null)
            {
                this.AdapterFactories = new List<AdapterFactory>();
            }

            this.AdapterFactories.Add(
                new AdapterFactory(
                    typeof(TAdapter).Name,
                    "semver:" + typeof(TAdapter).Assembly.GetName().Version,
                    adapterFactory));

            return this;
        }

        /// <summary>
        /// Configures tracing options.
        /// </summary>
        /// <param name="options">Instance of <see cref="TracerConfiguration"/>.</param>
        /// <returns>Returns <see cref="TracerBuilder"/> for chaining.</returns>
        public TracerBuilder SetTracerOptions(TracerConfiguration options)
        {
            this.TracerConfigurationOptions = options ?? throw new ArgumentNullException(nameof(options));
            return this;
        }

        internal readonly struct AdapterFactory
        {
            public readonly string Name;
            public readonly string Version;
            public readonly Func<Tracer, object> Factory;

            internal AdapterFactory(string name, string version, Func<Tracer, object> factory)
            {
                this.Name = name;
                this.Version = version;
                this.Factory = factory;
            }
        }
    }
}
