namespace NCoreUtils.Logging

open System
open System.Runtime.CompilerServices
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open NCoreUtils
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
    let config = { ProjectId = null; ServiceName = null; ServiceVersion = null; EnvPodName = null; EnvNodeName = null }
    configuration.Bind config
    LoggerFactoryGoogleExtensions.AddGoogleSink (factory, httpContextAccessor, config)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member AddGoogleSink (factory : ILoggerFactory, httpContextAccessor, projectId, serviceName, serviceVersion, envPodName, envNodeName) =
    let config = { ProjectId = projectId; ServiceName = serviceName; ServiceVersion = serviceVersion; EnvPodName = envPodName; EnvNodeName = envNodeName }
    LoggerFactoryGoogleExtensions.AddGoogleSink (factory, httpContextAccessor, config)


  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member AddGoogleSink (factory : ILoggerFactory, httpContextAccessor, projectId, serviceName, serviceVersion) =
    LoggerFactoryGoogleExtensions.AddGoogleSink (factory, httpContextAccessor, projectId, serviceName, serviceVersion, null, null)

  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member private AddGoogleSink (factory : ILoggerFactory, httpContextAccessor, builder : GoogleAspNetCoreLoggingExtendedConfigurationBuilder, configure : Action<GoogleAspNetCoreLoggingExtendedConfigurationBuilder>) =
    if not (isNull configure) then
      configure.Invoke builder
    let config = GoogleAspNetCoreLoggingExtendedConfiguration builder
    LoggerFactoryGoogleExtensions.AddGoogleSink (factory, httpContextAccessor, config)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member AddGoogleSink (factory : ILoggerFactory, httpContextAccessor, configuration : IConfiguration, configure : Action<GoogleAspNetCoreLoggingExtendedConfigurationBuilder>) =
    let builder = GoogleAspNetCoreLoggingExtendedConfigurationBuilder.FromConfiguration configuration
    LoggerFactoryGoogleExtensions.AddGoogleSink (factory, httpContextAccessor, builder, configure)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member AddGoogleSink (factory : ILoggerFactory, httpContextAccessor, projectId, serviceName, serviceVersion, envPodName, envNodeName, configure : Action<GoogleAspNetCoreLoggingExtendedConfigurationBuilder>) =
    let config = { ProjectId = projectId; ServiceName = serviceName; ServiceVersion = serviceVersion; EnvPodName = envPodName; EnvNodeName = envNodeName }
    let builder = GoogleAspNetCoreLoggingExtendedConfigurationBuilder.FromConfiguration config
    LoggerFactoryGoogleExtensions.AddGoogleSink (factory, httpContextAccessor, builder, configure)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member AddGoogleSink (factory : ILoggerFactory, httpContextAccessor, projectId, serviceName, serviceVersion, configure : Action<GoogleAspNetCoreLoggingExtendedConfigurationBuilder>) =
    LoggerFactoryGoogleExtensions.AddGoogleSink (factory, httpContextAccessor, projectId, serviceName, serviceVersion, null, null, configure)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member AddGoogleSink (factory : ILoggerFactory, httpContextAccessor, projectId, serviceName, configure : Action<GoogleAspNetCoreLoggingExtendedConfigurationBuilder>) =
    factory.AddGoogleSink (httpContextAccessor, projectId, serviceName, null, configure)
