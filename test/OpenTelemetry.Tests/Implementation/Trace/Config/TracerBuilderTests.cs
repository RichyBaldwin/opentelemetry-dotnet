﻿// <copyright file="TracerBuilderTests.cs" company="OpenTelemetry Authors">
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
using System.Linq;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Resources;
using OpenTelemetry.Testing.Export;
using OpenTelemetry.Trace;
using OpenTelemetry.Trace.Configuration;
using OpenTelemetry.Trace.Export;
using OpenTelemetry.Trace.Samplers;
using Xunit;

namespace OpenTelemetry.Tests.Impl.Trace
{
    public class TracerBuilderTests
    {
        [Fact]
        public void TracerBuilder_BadArgs()
        {
            Assert.Throws<ArgumentNullException>(() => new TracerBuilder().SetSampler(null));
            Assert.Throws<ArgumentNullException>(() => new TracerBuilder().AddProcessorPipeline(null));
            Assert.Throws<ArgumentNullException>(() => new TracerBuilder().SetTracerOptions(null));
            Assert.Throws<ArgumentNullException>(() => new TracerBuilder().AddAdapter<object>(null));
        }

        [Fact]
        public void TracerBuilder_Defaults()
        {
            var builder = new TracerBuilder();
            Assert.Null(builder.Sampler);
            Assert.Null(builder.ProcessingPipelines);
            Assert.Null(builder.TracerConfigurationOptions);
            Assert.Null(builder.AdapterFactories);
        }

        [Fact]
        public void TracerBuilder_ValidArgs()
        {
            var builder = new TracerBuilder();

            bool processorFactoryCalled = false;
            bool adapterFactoryCalled = true;

            var sampler = new ProbabilitySampler(0.1);
            var exporter = new TestExporter(_ => { });
            var options = new TracerConfiguration(1, 1, 1);

            builder
                .SetSampler(sampler)
                .AddProcessorPipeline(p => p
                    .SetExporter(exporter)
                    .SetExportingProcessor(e =>
                    {
                        processorFactoryCalled = true;
                        Assert.Same(e, exporter);
                        return new SimpleSpanProcessor(e);
                    }))
                .SetTracerOptions(options)
                .AddAdapter(t =>
                {
                    Assert.NotNull(t);
                    return new TestAdapter(t);
                });

            Assert.Same(sampler, builder.Sampler);

            Assert.NotNull(builder.ProcessingPipelines);
            Assert.Single(builder.ProcessingPipelines);
            Assert.Same(exporter, builder.ProcessingPipelines[0].Exporter);

            Assert.NotNull(builder.ProcessingPipelines[0].Build());
            Assert.True(processorFactoryCalled);

            Assert.Same(options, builder.TracerConfigurationOptions);
            Assert.Single(builder.AdapterFactories);

            var adapterFactory = builder.AdapterFactories.Single();
            Assert.Equal(nameof(TestAdapter), adapterFactory.Name);
            Assert.Equal("semver:" + typeof(TestAdapter).Assembly.GetName().Version, adapterFactory.Version);

            Assert.NotNull(adapterFactory.Factory);
            adapterFactory.Factory(new TracerSdk(new SimpleSpanProcessor(exporter), new AlwaysOnSampler(), options, Resource.Empty));

            Assert.True(adapterFactoryCalled);
        }

        private class TestAdapter
        {
            private readonly Tracer tracer;
            public TestAdapter(Tracer tracer)
            {
                this.tracer = tracer;
            }
        }
    }
}
