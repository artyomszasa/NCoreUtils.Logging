using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Google.Api;
using Google.Cloud.Logging.V2;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NCoreUtils.Logging.Google;
using Xunit;

namespace NCoreUtils.Logging.Unit
{
    public class GoogleSinkTests
    {
        public static async Task AspNetCoreSimpleBase(bool prePopulateContext)
        {
            var client = FakeGoogleLoggingClient.Create();
            var json = Encoding.ASCII.GetBytes(@"{
                ""ProjectId"": ""test"",
                ""Service"": ""test"",
                ""ServiceVersion"": ""3.1.0"",
                ""CategoryHandling"": ""IncludeAsLabel"",
                ""EventIdHandling"": ""IncludeValidIds"",
                ""TraceHandling"": ""Enabled"",
                ""ResourceType"": ""TEST"",
                ""ResourceLabels"": {
                    ""ResourceLabel"": ""ResourceLabelValue""
                }
            }");
            var configuration = new ConfigurationBuilder()
                .AddJsonStream(new MemoryStream(json))
                .Build();
            var services = new ServiceCollection()
                .AddTransient<LoggingServiceV2Client>(_ => client.Instance)
                .AddLogging(b => b
                    .ClearProviders()
                    .SetMinimumLevel(LogLevel.Debug)
                    .AddGoogleClient<AspNetCoreLoggerProvider>(configuration)
                );
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
            {
                var provider = context.Scope.ServiceProvider.GetRequiredService<ILoggerProvider>();
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
                        logger.LogError(new EventId(i), exn, "error");
                    }
                    while (client.WrittenEntries.Count < 2 * (i + 1))
                    {
                        await Task.Delay(100);
                    }
                }
                Assert.Equal(4, client.WrittenEntries.Count);
                client.ShouldThrow = true;
                logger.LogDebug("never seen...");
                Assert.Equal(4, client.WrittenEntries.Count);
                context.Scope.Dispose();
                await context.ServiceProvider.DisposeAsync();
            }
        }

        [Fact]
        public Task AspNetCoreSimpleNoPrePopulateContext()
        {
            Environment.SetEnvironmentVariable("ASPNET_CORE_ENVIRONMENT", "Development");
            return AspNetCoreSimpleBase(false);
        }

        [Fact]
        public Task AspNetCoreSimpleWithPrePopulateContext()
            => AspNetCoreSimpleBase(true);
    }
}