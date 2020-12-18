using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace NCoreUtils.Logging.Unit
{
    public sealed class FakeHttpContext : IHttpContextAccessor, IDisposable
    {
        sealed class FakeConnectionInfo : ConnectionInfo
        {
            public override X509Certificate2 ClientCertificate { get; set; } = default!;
            public override string Id { get; set; } = "xxx";
            public override IPAddress LocalIpAddress { get; set; } = IPAddress.Loopback;
            public override int LocalPort { get; set; } = 80;
            public override IPAddress RemoteIpAddress { get; set; } = IPAddress.Loopback;
            public override int RemotePort { get; set; } = 65532;

            public override Task<X509Certificate2> GetClientCertificateAsync(CancellationToken cancellationToken = default)
                => Task.FromResult<X509Certificate2>(default!);
        }

        private class FakeHttpContextAccessor : IHttpContextAccessor
        {
            public HttpContext HttpContext { get; set; } = default!;
        }

        public static FakeHttpContext Create(
            string method,
            string scheme,
            HostString host,
            PathString path,
            QueryString query,
            IHeaderDictionary headers,
            IServiceCollection services)
        {
            var connectionInfo = new FakeConnectionInfo();
            var requestBuilder = new Mock<HttpRequest>();
            requestBuilder.SetupGet(req => req.Method).Returns(method);
            requestBuilder.SetupGet(req => req.Scheme).Returns(scheme);
            requestBuilder.SetupGet(req => req.Host).Returns(host);
            requestBuilder.SetupGet(req => req.Path).Returns(path);
            requestBuilder.SetupGet(req => req.QueryString).Returns(query);
            requestBuilder.SetupGet(req => req.Headers).Returns(headers);
            var request = requestBuilder.Object;

            var responseBuilder = new Mock<HttpResponse>();
            responseBuilder.SetupProperty(resp => resp.StatusCode, 200);
            var response = responseBuilder.Object;

            var httpContextAccessor = new FakeHttpContextAccessor();
            services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);
            var items = new Dictionary<object, object>();
            var serviceProvider = services.BuildServiceProvider(true);
            var scope = serviceProvider.CreateScope();
            var contextBuilder = new Mock<HttpContext>();
            contextBuilder.SetupGet(ctx => ctx.Items).Returns(items);
            contextBuilder.SetupGet(ctx => ctx.Request).Returns(request);
            contextBuilder.SetupGet(ctx => ctx.Response).Returns(response);
            contextBuilder.SetupGet(ctx => ctx.RequestServices).Returns(scope.ServiceProvider);
            contextBuilder.SetupGet(ctx => ctx.Connection).Returns(connectionInfo);
            contextBuilder.SetupProperty(ctx => ctx.User, null!);
            var context = contextBuilder.Object;
            httpContextAccessor.HttpContext = context;
            return new FakeHttpContext(context, request, response, serviceProvider, scope);
        }

        public HttpContext HttpContext { get; }

        public HttpRequest HttpRequest { get; }

        public HttpResponse HttpResponse { get; }

        public ServiceProvider ServiceProvider { get; }

        public IServiceScope Scope { get; }

        HttpContext IHttpContextAccessor.HttpContext { get => HttpContext; set { return; } }

        private FakeHttpContext(HttpContext httpContext, HttpRequest httpRequest, HttpResponse httpResponse, ServiceProvider serviceProvider, IServiceScope scope)
        {
            HttpContext = httpContext;
            HttpRequest = httpRequest;
            HttpResponse = httpResponse;
            ServiceProvider = serviceProvider;
            Scope = scope;
        }

        public void Dispose() => Scope.Dispose();
    }
}