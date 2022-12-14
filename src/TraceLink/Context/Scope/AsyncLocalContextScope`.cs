using System.Threading;
using TraceLink.Abstractions.Context.Accessors;

namespace TraceLink.Abstractions.Context.Scope
{
    /// <inheritdoc cref="IContextAccessor{TContext}"/>
    public sealed class AsyncLocalContextScope<TContext> : IContextAccessor<TContext>, IContextScopeSetter<TContext> where TContext : class
    {
        private static readonly AsyncLocal<IContextScope<TContext>> _internalScope = new AsyncLocal<IContextScope<TContext>>();

        /// <inheritdoc/>
        public IContextScope Scope => _internalScope.Value!;

        /// <inheritdoc/>
        public TContext Context => _internalScope.Value!.Context;

        public void SetScope(IContextScope<TContext> contextScope)
            => _internalScope.Value = contextScope;
    }
}
