using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace NCoreUtils.Logging.Unit
{
    public class GoogleFluentdSinkTests
    {
        private sealed class TcpReader : IDisposable
        {
            private readonly StringBuilder _buffer = new();

            private readonly CancellationTokenSource _cancellation = new();

            private readonly TcpListener _listener;

            private readonly Task _task;

            public int Port { get; }

            public TcpReader()
            {
                _listener = new TcpListener(IPAddress.Any, 0);
                _listener.Start();
                Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
                _task = Run();
            }

            private async Task Run()
            {
                while (!_cancellation.IsCancellationRequested)
                {
                    try
                    {
                        using var client = await _listener.AcceptTcpClientAsync();
                        using var subscription = _cancellation.Token.Register(() => client.Dispose());
                        using var stream = client.GetStream();
                        using var reader = new StreamReader(stream, new UTF8Encoding(false), false);
                        for (var line = await reader.ReadLineAsync(); line != null; line = await reader.ReadLineAsync())
                        {
                            _buffer.AppendLine(line);
                        }
                    }
                    catch (ObjectDisposedException) { }
                    catch (OperationCanceledException) { }
                    catch (SocketException) { }
                }
            }

            public string Complete()
            {
                _cancellation.Cancel();
                _listener.Stop();
                return _buffer.ToString();
            }

            public void Dispose()
            {
                Complete();
                try
                {
                    _task.Wait();
                }
                catch { }
            }
        }

        public static async Task AspNetCoreSimpleBase(bool prePopulateContext)
        {
            string output;
            {
                using var tcpReader = new TcpReader();

                var json = Encoding.ASCII.GetBytes(@"{
                    ""Output"": ""tcp://127.0.0.1:%PORT%"",
                    ""ProjectId"": ""test"",
                    ""Service"": ""test"",
                    ""ServiceVersion"": ""3.1.0"",
                    ""CategoryHandling"": ""IncludeAsLabel"",
                    ""EventIdHandling"": ""IncludeValidIds"",
                    ""TraceHandling"": ""Enabled""
                }".Replace("%PORT%", tcpReader.Port.ToString()));
                var configuration = new ConfigurationBuilder()
                    .AddJsonStream(new MemoryStream(json))
                    .Build();
                var services = new ServiceCollection()
                    .AddDefaultTraceIdProvider()
                    .AddLogging(b => b
                        .ClearProviders()
                        .SetMinimumLevel(LogLevel.Debug)
                        .AddGoogleFluentd<AspNetCoreLoggerProvider>(configuration)
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
                }
                context.Scope.Dispose();
                await context.ServiceProvider.DisposeAsync();
                output = tcpReader.Complete();
            }
            var serializerOptions = Google.Data.LogEntryJsonContext.Default.Options;
            var entries = JsonSerializer.Deserialize<List<Google.Data.LogEntry>>("[" + output.Replace("}\n", "},\n").TrimEnd('\n', '\r', ',') + "]", serializerOptions)!;
            Assert.Equal(4, entries.Count);
            Assert.Equal(2, entries.Count(e => e.Message == "message"));
        }

        [Fact]
        public Task AspNetCoreSimpleNoPrePopulateContext()
        {
            Environment.SetEnvironmentVariable("ASPNET_CORE_ENVIRONMENT", "Development");
            return AspNetCoreSimpleBase(prePopulateContext: false);
        }

        [Fact]
        public Task AspNetCoreSimpleWithPrePopulateContext()
            => AspNetCoreSimpleBase(prePopulateContext: true);
    }
}