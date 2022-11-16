﻿using DeltaWare.SDK.Correlation.Forwarder;
using DeltaWare.SDK.Correlation.Options;
using NServiceBus.Pipeline;
using System;
using System.Threading.Tasks;

namespace DeltaWare.SDK.Correlation.NServiceBus.Behaviors
{
    internal class AttachContextIdBehavior : Behavior<IOutgoingPhysicalMessageContext>
    {
        private readonly IIdForwarder _idForwarder;

        private readonly IOptions _options;

        public AttachContextIdBehavior(IIdForwarder idForwarder, IOptions options)
        {
            _idForwarder = idForwarder;
            _options = options;
        }

        public override Task Invoke(IOutgoingPhysicalMessageContext context, Func<Task> next)
        {
            AttachHeader(context);

            return next.Invoke();
        }

        private void AttachHeader(IOutgoingPhysicalMessageContext context)
        {
            if (context.Headers.ContainsKey(_options.Key))
            {
                return;
            }

            string forwardingId = _idForwarder.GetForwardingId();

            context.Headers.Add(_options.Key, forwardingId);
        }
    }
}