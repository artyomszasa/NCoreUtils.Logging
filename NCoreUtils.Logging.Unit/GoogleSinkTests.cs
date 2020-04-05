using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Api;
using Google.Cloud.Logging.V2;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NCoreUtils.AspNetCore;
using NCoreUtils.Logging.Google;
using Xunit;

namespace NCoreUtils.Logging.Unit
{
    public class GoogleSinkTests
    {
        private class AspNetCoreSink : AspNetCoreGoogleSink
        {
            readonly LoggingServiceV2Client _client;

            public AspNetCoreSink(AspNetCoreGoogleLoggingContext context, LoggingServiceV2Client client)
                : base(context)
            {
                _client = client;
            }

            protected override ValueTask<LoggingServiceV2Client> GetClientAsync(CancellationToken cancellationToken)
                => new ValueTask<LoggingServiceV2Client>(_client);
        }

        public async Task AspNetCoreSimpleBase(bool prePopulateContext)
        {
            var conf = new AspNetCoreGoogleLoggingContext(
                logName: new LogName("test", "test"),
                resource: new MonitoredResource { Type = "global" },
                serviceVersion: default
            );
            var services = new ServiceCollection()
                .AddSingleton(conf);
            if (prePopulateContext)
            {
                services.AddLoggingContext();
            }

            using var context = FakeHttpContext.Create(
                method: "GET",
                scheme: "http",
                host: new HostString("localhost"),
                path: "/index.html",
                query: QueryString.Empty,
                headers: new HeaderDictionary(),
                services: services
            );
            if (prePopulateContext)
            {
                context.Scope.ServiceProvider.GetRequiredService<LoggingContext>().PopulateFrom(context.HttpContext);
            }
            var client = FakeGoogleLoggingClient.Create();
            {
                await using var sink = new AspNetCoreSink(conf, client.Instance);
                await using var provider = new AspNetCoreLoggerProvider(sink, context);
                var logger = provider.CreateLogger("category");
                for (var i = 0; i < 2; ++i)
                {
                    logger.LogInformation("message");
                    try
                    {
                        throw new Exception("dummy");
                    }
                    catch (Exception exn)
                    {
                        logger.LogError(exn, "error");
                    }
                    while (client.WrittenEntries.Count < 2 * (i + 1))
                    {
                        await Task.Delay(100);
                    }
                }
            }
            Assert.Equal(4, client.WrittenEntries.Count);
        }

        [Fact]
        public Task AspNetCoreSimpleNoPrePopulateContext()
            => AspNetCoreSimpleBase(false);

        [Fact]
        public Task AspNetCoreSimpleWithPrePopulateContext()
            => AspNetCoreSimpleBase(true);
    }
}