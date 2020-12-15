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
    public class GoogleFluentdSinkTests
    {
        private sealed class TcpReader : IDisposable
        {
            private readonly StringBuilder _buffer = new StringBuilder();

            private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();

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

        public async Task AspNetCoreSimpleBase(bool prePopulateContext)
        {
            string output;
            {
                using var tcpReader = new TcpReader();
                var conf = new AspNetCoreGoogleFluentdLoggingContext(
                    fluentdUri: $"tcp://127.0.0.1:{tcpReader.Port}",
                    projectId: "test",
                    logId: "test",
                    serviceVersion: default
                );
                var services = new ServiceCollection()
                    .AddOptions<GoogleFluentdOptions>()
                        .Services
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
                {
                    await using var sink = ActivatorUtilities.CreateInstance<AspNetCoreGoogleFluentdSink>(context.Scope.ServiceProvider);
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
                    }
                }
                output = tcpReader.Complete();
            }
            var serializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                AllowTrailingCommas = true,
                Converters = {
                    ImmutableJsonConverterFactory.GetOrCreate<Google.Data.LogEntry>(),
                    ImmutableJsonConverterFactory.GetOrCreate<Google.Data.ServiceContext>(),
                    ImmutableJsonConverterFactory.GetOrCreate<Google.Data.ErrorContext>(),
                    ImmutableJsonConverterFactory.GetOrCreate<Google.Data.HttpRequest>(),
                    new Google.Data.SeverityConverter(),
                    new Google.Data.TimestampConverter()
                }
            };
            var entries = JsonSerializer.Deserialize<List<Google.Data.LogEntry>>("[" + output.Replace("}\n", "},\n") + "]", serializerOptions);
            Assert.Equal(4, entries.Count);
            Assert.Equal(2, entries.Count(e => e.Message == "message"));
        }

        [Fact]
        public Task AspNetCoreSimpleNoPrePopulateContext()
            => AspNetCoreSimpleBase(false);

        [Fact]
        public Task AspNetCoreSimpleWithPrePopulateContext()
            => AspNetCoreSimpleBase(true);
    }
}