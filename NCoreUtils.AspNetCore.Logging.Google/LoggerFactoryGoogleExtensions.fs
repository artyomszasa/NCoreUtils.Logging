namespace NCoreUtils.Logging

open System.Runtime.CompilerServices
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open NCoreUtils.Logging.Google

[<Extension>]
[<Sealed; AbstractClass>]
type LoggerFactoryGoogleExtensions =

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member AddGoogleSink (factory : ILoggerFactory, httpContextAccessor, config : IGoogleAspNetCoreLoggingConfiguration) =
    let sink = new GoogleAspNetCoreSink (config)
    factory.AddProvider(new AspNetCoreLoggerProvider(sink, httpContextAccessor))
    factory

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member AddGoogleSink (factory : ILoggerFactory, httpContextAccessor, configuration : IConfiguration) =
    let config = { ProjectId = null; ServiceName = null; ServiceVersion = null }
    configuration.Bind config
    LoggerFactoryGoogleExtensions.AddGoogleSink (factory, httpContextAccessor, config)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member AddGoogleSink (factory : ILoggerFactory, httpContextAccessor, projectId, serviceName, serviceVersion) =
    let config = { ProjectId = projectId; ServiceName = serviceName; ServiceVersion = serviceVersion }
    LoggerFactoryGoogleExtensions.AddGoogleSink (factory, httpContextAccessor, config)