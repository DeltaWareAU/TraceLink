﻿using DeltaWare.SDK.Correlation.AspNetCore.Context.Scopes;
using DeltaWare.SDK.Correlation.AspNetCore.Filters;
using DeltaWare.SDK.Correlation.AspNetCore.Handler;
using DeltaWare.SDK.Correlation.Context.Accessors;
using DeltaWare.SDK.Correlation.Options;
using DeltaWare.SDK.Correlation.Providers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;

namespace DeltaWare.SDK.Correlation.AspNetCore.Options.Builder
{
    public sealed class TraceOptionsBuilder
    {
        public IServiceCollection Services { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks><b>Default value:</b> x-trace-id</remarks>
        public string Header { get; set; } = "x-trace-id";

        internal TraceOptionsBuilder(IServiceCollection services)
        {
            Services = services;
        }

        internal void InternalBuild()
        {
            Services.TryAddScoped<AspNetTraceContextScope>();
            Services.TryAddSingleton<TraceContextAccessor>();

            Services.TryAddSingleton<ITraceContextAccessor>(p => p.GetRequiredService<TraceContextAccessor>());

            Services.TryAddSingleton<ITraceIdProvider, GuidTraceIdProvider>();
            Services.TryAddSingleton<ITraceOptions>(new TraceOptions
            {
                Header = Header
            });

            TryAddFilter(Services);
            TryAddHandler(Services);
        }

        private static void TryAddFilter(IServiceCollection services)
        {
            if (services.Any(s => s.ServiceType == typeof(TraceIdSetFilter)))
            {
                return;
            }

            services.AddScoped<TraceIdSetFilter>();
            services.Configure<MvcOptions>(o =>
            {
                o.Filters.AddService<TraceIdSetFilter>();
            });
        }

        private static void TryAddHandler(IServiceCollection services)
        {
            if (services.Any(x => x.ServiceType == typeof(TraceIdHandler)))
            {
                return;
            }

            services.AddSingleton<TraceIdHandler>();
            services.Configure<HttpMessageHandlerBuilder>(c =>
            {
                c.AdditionalHandlers.Add(c.Services.GetRequiredService<TraceIdHandler>());
            });
        }
    }
}