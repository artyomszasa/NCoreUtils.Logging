using System;
using Microsoft.AspNetCore.Http;

namespace NCoreUtils.Logging
{
    public class AspNetCoreLoggerProvider : LoggerProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AspNetCoreLoggerProvider(ISink sink, IHttpContextAccessor? httpContextAccessor = default)
            : base(sink)
        {
            if (httpContextAccessor is null)
            {
                throw new InvalidOperationException("No http context accessor found. Add it using services.AddHttpContextAccessor().");
            }
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Logger DoCreateLogger(string categoryName)
            => new AspNetCoreLogger(this, categoryName, _httpContextAccessor);
    }
}