namespace NCoreUtils.Logging

open System
open System.Runtime.CompilerServices
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open NCoreUtils
open NCoreUtils.Logging.Google

[<Sealed>]
type GoogleAspNetCoreLoggingExtendedConfigurationBuilder internal (basicConfiguration : GoogleAspNetCoreLoggingConfiguration) =
  let labelSources = ResizeArray ()
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member FromConfiguration (basicConfiguration : GoogleAspNetCoreLoggingConfiguration) =
    GoogleAspNetCoreLoggingExtendedConfigurationBuilder basicConfiguration
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member FromConfiguration (section : IConfiguration) =
    let basicConfiguration = section.Get<GoogleAspNetCoreLoggingConfiguration> ()
    if box basicConfiguration |> isNull then
      invalidOpf "Unable to create basic configuration from configuration section."
    GoogleAspNetCoreLoggingExtendedConfigurationBuilder.FromConfiguration basicConfiguration
  member internal __.BasicConfiguration = basicConfiguration
  member internal __.LabelSources = labelSources
  member this.AddLabels (factory : Action<DateTimeOffset, string, LogLevel, EventId, AspNetCoreContext, Action<string, string>>) =
    labelSources.Add factory
    this
  member this.AddLabels (factory : Action<AspNetCoreContext, Action<string, string>>) =
    let factory' = Action<_, _, _, _, _, _> (fun _ _ _ _ context add -> factory.Invoke (context, add))
    labelSources.Add factory'
    this

[<Sealed>]
type private GoogleAspNetCoreLoggingExtendedConfiguration (builder : GoogleAspNetCoreLoggingExtendedConfigurationBuilder) =
  member val ProjectId      = builder.BasicConfiguration.ProjectId
  member val ServiceName    = builder.BasicConfiguration.ServiceName
  member val ServiceVersion = builder.BasicConfiguration.ServiceVersion
  member val LabelFactories = builder.LabelSources |> Seq.toList
  with
    interface IGoogleAspNetCoreLoggingConfiguration with
      member this.ProjectId      = this.ProjectId
      member this.ServiceName    = this.ServiceName
      member this.ServiceVersion = this.ServiceVersion
      member this.PopulateLabels (timestamp, category, logLevel, eventId, context, addLabel) =
        this.LabelFactories
        |> List.iter (fun factory -> factory.Invoke (timestamp, category, logLevel, eventId, context, addLabel))


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

  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member private AddGoogleSink (factory : ILoggerFactory, httpContextAccessor, builder : GoogleAspNetCoreLoggingExtendedConfigurationBuilder, configure : Action<GoogleAspNetCoreLoggingExtendedConfigurationBuilder>) =
    if not (isNull configure) then
      configure.Invoke builder
    let config = GoogleAspNetCoreLoggingExtendedConfiguration builder
    LoggerFactoryGoogleExtensions.AddGoogleSink (factory, httpContextAccessor, config)

  [<Extension>]
  static member AddGoogleSink (factory : ILoggerFactory, httpContextAccessor, configuration : IConfiguration, configure : Action<GoogleAspNetCoreLoggingExtendedConfigurationBuilder>) =
    let builder = GoogleAspNetCoreLoggingExtendedConfigurationBuilder.FromConfiguration configuration
    LoggerFactoryGoogleExtensions.AddGoogleSink (factory, httpContextAccessor, builder, configure)

  [<Extension>]
  static member AddGoogleSink (factory : ILoggerFactory, httpContextAccessor, projectId, serviceName, serviceVersion, configure : Action<GoogleAspNetCoreLoggingExtendedConfigurationBuilder>) =
    let config = { ProjectId = projectId; ServiceName = serviceName; ServiceVersion = serviceVersion }
    let builder = GoogleAspNetCoreLoggingExtendedConfigurationBuilder.FromConfiguration config
    LoggerFactoryGoogleExtensions.AddGoogleSink (factory, httpContextAccessor, builder, configure)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member AddGoogleSink (factory : ILoggerFactory, httpContextAccessor, projectId, serviceName, configure : Action<GoogleAspNetCoreLoggingExtendedConfigurationBuilder>) =
    factory.AddGoogleSink (httpContextAccessor, projectId, serviceName, null, configure)
