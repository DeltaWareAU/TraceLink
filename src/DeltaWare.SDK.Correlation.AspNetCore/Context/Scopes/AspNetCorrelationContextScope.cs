﻿using DeltaWare.SDK.Correlation.AspNetCore.Attributes;
using DeltaWare.SDK.Correlation.AspNetCore.Extensions;
using DeltaWare.SDK.Correlation.Context;
using DeltaWare.SDK.Correlation.Context.Accessors;
using DeltaWare.SDK.Correlation.Options;
using DeltaWare.SDK.Correlation.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DeltaWare.SDK.Correlation.AspNetCore.Context.Scopes
{
    internal sealed class AspNetCorrelationContextScope : BaseAspNetContextScope<CorrelationContext>
    {
        public override CorrelationContext Context { get; }
        public override bool DidReceiveContextId { get; }
        public override string ContextId => Context.CorrelationId;

        public AspNetCorrelationContextScope(ContextAccessor<CorrelationContext> contextAccessor, IOptions<CorrelationContext> options, IIdProvider<CorrelationContext> idProvider, IHttpContextAccessor httpContextAccessor, ILogger? logger = null) : base(contextAccessor, options, idProvider, httpContextAccessor, logger)
        {
            if (!TryGetId(out string? correlationId))
            {
                DidReceiveContextId = false;

                correlationId = idProvider.GenerateId();

                Logger?.LogTrace("No CorrelationId was attached to the RequestHeaders. A new CorrelationId has been generated. {CorrelationId}", correlationId);
            }
            else
            {
                DidReceiveContextId = true;

                Logger?.LogDebug("A CorrelationId {CorrelationId} was attached to the RequestHeaders.", correlationId);
            }

            Context = new CorrelationContext(correlationId!);

            if (options.AttachToResponse)
            {
                TrySetId();
            }
        }

        protected override void OnMultipleIdsFounds(string[] foundIds)
        {
            Logger?.LogWarning("Multiple CorrelationIds found ({CorrelationIds}), only the first value will be used.", string.Join(',', foundIds));
        }

        protected override void OnIdAttached(string id)
        {
            Logger?.LogDebug("Correlation ID {CorrelationId} has been attached to the Response Headers", id);
        }

        protected override bool CanSkipValidation(HttpContext context)
        {
            if (!context.Features.HasFeature<CorrelationIdHeaderNotRequiredAttribute>())
            {
                return false;
            }

            Logger?.LogTrace("Key Validation will be skipped as the CorrelationIdHeaderNotRequiredAttribute is present.");

            return true;
        }

        protected override void OnValidationPassed()
        {
            Logger?.LogDebug("Key Validation Passed. A CorrelationId {CorrelationId} was received in the HttpRequest Headers", Context.CorrelationId);
        }

        protected override void OnValidationFailed()
        {
            Logger?.LogWarning("Key Validation Failed. A CorrelationId was not received in the HttpRequest Headers, responding with 400 (Bad Request).");
        }
    }
}
