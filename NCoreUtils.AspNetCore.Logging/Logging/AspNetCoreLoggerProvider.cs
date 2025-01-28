using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Logging
{
    public class AspNetCoreLoggerProvider : LoggerProvider, ISupportExternalScope
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        internal IExternalScopeProvider? ExternalScopeProvider { get; private set;}

        public AspNetCoreLoggerProvider(ISink sink, IHttpContextAccessor? httpContextAccessor = default)
            : base(sink)
        {
            if (httpContextAccessor is null)
            {
                throw new InvalidOperationException("No http context accessor found. Add it using services.AddHttpContextAccessor().");
            }
            _httpContextAccessor = httpContextAccessor;
        }

        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            ExternalScopeProvider = scopeProvider;
        }

        protected override Logger DoCreateLogger(string categoryName)
            => new AspNetCoreLogger(this, categoryName, _httpContextAccessor);
    }
}