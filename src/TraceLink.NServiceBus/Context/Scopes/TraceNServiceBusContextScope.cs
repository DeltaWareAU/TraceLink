using Microsoft.Extensions.Logging;
using NServiceBus.Pipeline;
using TraceLink.Abstractions.Context;
using TraceLink.Abstractions.Context.Scope;
using TraceLink.Abstractions.Options;

namespace TraceLink.NServiceBus.Context.Scopes
{
    internal sealed class TraceNServiceBusContextScope : NServiceBusContextScope<TraceContext>
    {
        public override bool DidReceiveContextId { get; }
        public override string ContextId => Context.TraceId ?? string.Empty;
        public override TraceContext Context { get; }

        public TraceNServiceBusContextScope(IContextScopeSetter<TraceContext> contextScopeSetter, IOptions<TraceContext> options, IIncomingPhysicalMessageContext context, ILogger? logger = null) : base(contextScopeSetter, options, context, logger)
        {
            if (!TryGetId(out string? traceId))
            {
                DidReceiveContextId = false;

                logger?.LogDebug("No TraceId was attached to the Incoming Transport Message Headers.");

                Context = new TraceContext();
            }
            else
            {
                DidReceiveContextId = true;

                logger?.LogTrace("A TraceId {TraceId} was attached to the Incoming Transport Message Headers.", traceId);

                Context = new TraceContext(traceId!);
            }
        }

        protected override void OnValidationPassed()
        {
            Logger?.LogDebug("Header Validation Passed. A TraceId {TraceId} was received in the Incoming Transport Message Headers.", Context.TraceId);
        }

        protected override void OnValidationFailed()
        {
            Logger?.LogWarning("Header Validation Failed. A TraceId was not received in the Incoming Transport Message Headers.");
        }
    }
}
