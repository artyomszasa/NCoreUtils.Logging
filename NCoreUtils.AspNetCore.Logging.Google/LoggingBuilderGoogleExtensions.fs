namespace NCoreUtils.Logging

open System
open System.Runtime.CompilerServices
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open NCoreUtils.Logging.Google

type Configure = Action<GoogleAspNetCoreLoggingExtendedConfigurationBuilder>

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

  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member private AddGoogleSink (loggingBuilder : ILoggingBuilder, configurationBuilder : GoogleAspNetCoreLoggingExtendedConfigurationBuilder, configure : Configure) =
    if not (isNull configure) then
      configure.Invoke configurationBuilder
    let config = GoogleAspNetCoreLoggingExtendedConfiguration configurationBuilder
    LoggingBuilderGoogleExtensions.AddGoogleSink (loggingBuilder, config)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member AddGoogleSink (loggingBuilder : ILoggingBuilder, configuration : IConfiguration, configure : Configure) =
    let builder = GoogleAspNetCoreLoggingExtendedConfigurationBuilder.FromConfiguration configuration
    LoggingBuilderGoogleExtensions.AddGoogleSink (loggingBuilder, builder, configure)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member AddGoogleSink (loggingBuilder : ILoggingBuilder, projectId, serviceName, serviceVersion, envPodName, envNodeName, configure : Configure) =
    let config = { ProjectId = projectId; ServiceName = serviceName; ServiceVersion = serviceVersion; EnvPodName = envPodName; EnvNodeName = envNodeName }
    let builder = GoogleAspNetCoreLoggingExtendedConfigurationBuilder.FromConfiguration config
    LoggingBuilderGoogleExtensions.AddGoogleSink (loggingBuilder, builder, configure)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member AddGoogleSink (loggingBuilder : ILoggingBuilder, projectId, serviceName, serviceVersion, configure : Configure) =
    LoggingBuilderGoogleExtensions.AddGoogleSink (loggingBuilder, projectId, serviceName, serviceVersion, null, null, configure)
