namespace NCoreUtils.Logging

open System.Runtime.CompilerServices
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open NCoreUtils.Logging.Google

[<Extension>]
[<Sealed; AbstractClass>]
type LoggingBuilderGoogleExtensions =

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member AddGoogleSink (builder : ILoggingBuilder, config : IGoogleAspNetCoreLoggingConfiguration) =
    builder.Services.AddSingleton config |> ignore
    builder.AddSink<GoogleAspNetCoreSink> ()

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member AddGoogleSink (builder : ILoggingBuilder, configuration : IConfiguration) =
    let config = { ProjectId = null; ServiceName = null; ServiceVersion = null }
    configuration.Bind config
    LoggingBuilderGoogleExtensions.AddGoogleSink (builder, config)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member AddGoogleSink (builder : ILoggingBuilder, projectId, serviceName, serviceVersion) =
    let config = { ProjectId = projectId; ServiceName = serviceName; ServiceVersion = serviceVersion }
    LoggingBuilderGoogleExtensions.AddGoogleSink (builder, config)