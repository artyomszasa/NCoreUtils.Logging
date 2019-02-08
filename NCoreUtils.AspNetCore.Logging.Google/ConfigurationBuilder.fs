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
  member val EnvPodName     = builder.BasicConfiguration.EnvPodName
  member val EnvNodeName    = builder.BasicConfiguration.EnvNodeName
  member val LabelFactories = builder.LabelSources |> Seq.toList
  with
    interface IGoogleAspNetCoreLoggingConfiguration with
      member this.ProjectId      = this.ProjectId
      member this.ServiceName    = this.ServiceName
      member this.ServiceVersion = this.ServiceVersion
      member this.EnvPodName     = this.EnvPodName
      member this.EnvNodeName    = this.EnvNodeName
      member this.PopulateLabels (timestamp, category, logLevel, eventId, context, addLabel) =
        this.LabelFactories
        |> List.iter (fun factory -> factory.Invoke (timestamp, category, logLevel, eventId, context, addLabel))

