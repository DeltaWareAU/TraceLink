﻿using System;

namespace TraceLink.Abstractions.Context
{
    public sealed class RequestTransactionContext : ITracingContext
    {
        public RequestTransactionContext(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; }
    }
}
