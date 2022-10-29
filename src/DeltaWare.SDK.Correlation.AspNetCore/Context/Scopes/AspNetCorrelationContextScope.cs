﻿using DeltaWare.SDK.Correlation.Context;
using DeltaWare.SDK.Correlation.Context.Accessors;
using DeltaWare.SDK.Correlation.Context.Scope;
using DeltaWare.SDK.Correlation.Options;
using DeltaWare.SDK.Correlation.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace DeltaWare.SDK.Correlation.AspNetCore.Context.Scopes
{
    internal sealed class AspNetCorrelationContextScope : IContextScope<CorrelationContext>
    {
        private readonly ICorrelationOptions _options;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ICorrelationContextAccessor>? _logger;

        public CorrelationContext Context { get; }

        public AspNetCorrelationContextScope(ICorrelationOptions options, ICorrelationIdProvider idProvider, IHttpContextAccessor httpContextAccessor, ILogger<ICorrelationContextAccessor>? logger = null)
        {
            _options = options;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;

            if (!TryGetId(out string? correlationId))
            {
                correlationId = idProvider.GenerateId();

                _logger?.LogDebug("No CorrelationId was attached to the RequestHeaders. A new CorrelationId has been generated. {CorrelationId}", correlationId);
            }
            else
            {
                _logger?.LogDebug("A CorrelationId {CorrelationId} was attached to the RequestHeaders.", correlationId);
            }

            Context = new CorrelationContext(correlationId!);
        }

        public bool TryGetId(out string? idValue)
        {
            IHeaderDictionary headerDictionary = _httpContextAccessor.HttpContext.Request.Headers;

            if (!headerDictionary.TryGetValue(_options.Header, out StringValues values) || StringValues.IsNullOrEmpty(values))
            {
                idValue = null;

                return false;
            }

            string[] valueArray = values.ToArray();

            if (valueArray.Length > 1)
            {
                _logger?.LogWarning("Multiple CorrelationIds found ({CorrelationIds}), only the first value will be used.", values.ToString());
            }

            idValue = values.First();

            return true;
        }

        public bool TrySetId(string value)
        {
            _httpContextAccessor.HttpContext.Response.Headers.Add(_options.Header, value);

            return true;
        }

        public bool TrySetId() => TrySetId(Context.CorrelationId);
    }
}