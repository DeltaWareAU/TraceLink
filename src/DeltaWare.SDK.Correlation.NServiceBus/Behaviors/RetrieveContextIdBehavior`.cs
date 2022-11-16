﻿using DeltaWare.SDK.Correlation.NServiceBus.Context.Scopes;
using DeltaWare.SDK.Correlation.Options;
using Microsoft.Extensions.Logging;
using NServiceBus.Pipeline;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DeltaWare.SDK.Correlation.NServiceBus.Behaviors
{
    internal abstract class RetrieveContextIdBehavior<TContext> : Behavior<IIncomingPhysicalMessageContext> where TContext : class
    {
        private readonly IOptions _options;
        private readonly ILogger? _logger;

        protected RetrieveContextIdBehavior(IOptions options, ILogger? logger)
        {
            _options = options;
            _logger = logger;
        }

        public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
        {
            NServiceBusContextScope<TContext> contextScope = CreateContextScope(context);

            contextScope.ValidateHeader();

            if (_logger == null || !_options.AttachToLoggingScope)
            {
                await next();

                return;
            }

            Dictionary<string, string> state = new Dictionary<string, string>
            {
                [_options.LoggingScopeKey] = contextScope.ContextId
            };

            using (_logger.BeginScope(state))
            {
                await next();
            }
        }

        protected abstract NServiceBusContextScope<TContext> CreateContextScope(IIncomingPhysicalMessageContext context);
    }
}