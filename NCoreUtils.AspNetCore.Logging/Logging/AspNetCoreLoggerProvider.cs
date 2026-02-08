using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace NCoreUtils.Logging;

public class AspNetCoreLoggerProvider(ISink sink, IHttpContextAccessor? httpContextAccessor = default) : LoggerProvider(sink), ISupportExternalScope
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor
        ?? throw new InvalidOperationException("No http context accessor found. Add it using services.AddHttpContextAccessor().");

    internal IExternalScopeProvider? ExternalScopeProvider { get; private set;}

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        => ExternalScopeProvider = scopeProvider;

    protected override Logger DoCreateLogger(string categoryName)
        => new AspNetCoreLogger(this, categoryName, _httpContextAccessor);
}