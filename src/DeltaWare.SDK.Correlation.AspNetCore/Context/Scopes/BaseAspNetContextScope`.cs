﻿using DeltaWare.SDK.Correlation.Context.Accessors;
using DeltaWare.SDK.Correlation.Options;
using DeltaWare.SDK.Correlation.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Linq;
using System.Threading.Tasks;

namespace DeltaWare.SDK.Correlation.AspNetCore.Context.Scopes
{
    internal abstract class BaseAspNetContextScope<TContext> : IAspNetContextScope<TContext> where TContext : class

    {
        protected IOptions Options { get; }
        protected IHttpContextAccessor HttpContextAccessor { get; }
        protected ILogger? Logger { get; }

        public abstract TContext Context { get; }

        public abstract bool DidReceiveContextId { get; }

        public abstract string ContextId { get; }

        protected BaseAspNetContextScope(ContextAccessor<TContext> contextAccessor, IOptions<TContext> options,
            IIdProvider<TContext> idProvider, IHttpContextAccessor httpContextAccessor, ILogger? logger = null)
        {
            contextAccessor.InternalScope = this;

            Options = options;
            HttpContextAccessor = httpContextAccessor;
            Logger = logger;
        }

        public bool TryGetId(out string? idValue)
        {
            IHeaderDictionary headerDictionary = HttpContextAccessor.HttpContext.Request.Headers;

            if (!headerDictionary.TryGetValue(Options.Key, out StringValues values) ||
                StringValues.IsNullOrEmpty(values))
            {
                idValue = null;

                return false;
            }

            string[] valueArray = values.ToArray();

            if (valueArray.Length > 1)
            {
                OnMultipleIdsFounds(valueArray);
            }

            idValue = values.First();

            return true;
        }

        public void TrySetId(bool force = false)
            => TrySetId(ContextId);

        public void TrySetId(string idValue, bool force = false)
        {
            HttpContextAccessor.HttpContext.Response.OnStarting(() =>
            {
                if (HttpContextAccessor.HttpContext.Response.Headers.ContainsKey(Options.Key))
                {
                    return Task.CompletedTask;
                }

                if (!force && !ShouldAttachToResponse(HttpContextAccessor.HttpContext))
                {
                    return Task.CompletedTask;
                }

                HttpContextAccessor.HttpContext.Response.Headers.Add(Options.Key, idValue);

                OnIdAttached(idValue);

                return Task.CompletedTask;
            });
        }

        public async Task<bool> ValidateHeaderAsync(HttpContext context, bool force = false)
        {
            if (!force)
            {
                if (CanSkipValidation(context) || !IsValidationRequired(context))
                {
                    Logger?.LogTrace("Header Validation will be skipped as it is not required.");

                    return true;
                }
            }
            else
            {
                Logger?.LogTrace("Header Validation will done as it has been forced.");
            }
            
            if (DidReceiveContextId)
            {
                OnValidationPassed();

                return true;
            }

            OnValidationFailed();

            context.Response.StatusCode = StatusCodes.Status400BadRequest;

            await context.Response.WriteAsync($"The Request Headers must contain the \"{Options.Key}\" Key.");

            return false;
        }

        protected virtual bool IsValidationRequired(HttpContext context)
            => Options.IsRequired;

        protected virtual bool ShouldAttachToResponse(HttpContext context)
            => Options.AttachToResponse;

        protected abstract void OnMultipleIdsFounds(string[] foundIds);

        protected abstract void OnIdAttached(string id);

        protected abstract bool CanSkipValidation(HttpContext context);

        protected abstract void OnValidationPassed();

        protected abstract void OnValidationFailed();
    }
}
