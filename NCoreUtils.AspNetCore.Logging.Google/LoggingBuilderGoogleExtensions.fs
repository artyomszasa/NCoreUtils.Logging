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
    let config = { ProjectId = null; ServiceName = null; ServiceVersion = null; EnvPodName = null; EnvNodeName = null }
    configuration.Bind config
    LoggingBuilderGoogleExtensions.AddGoogleSink (builder, config)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member AddGoogleSink (builder : ILoggingBuilder, projectId, serviceName, serviceVersion, envPodName, envNodeName) =
    let config = { ProjectId = projectId; ServiceName = serviceName; ServiceVersion = serviceVersion; EnvPodName = envPodName; EnvNodeName = envNodeName }
    LoggingBuilderGoogleExtensions.AddGoogleSink (builder, config)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member AddGoogleSink (builder : ILoggingBuilder, projectId, serviceName, serviceVersion) =
    LoggingBuilderGoogleExtensions.AddGoogleSink (builder, projectId, serviceName, serviceVersion, null, null)