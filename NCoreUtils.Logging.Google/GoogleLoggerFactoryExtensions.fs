namespace NCoreUtils.Logging

open Microsoft.Extensions.Logging
open Microsoft.Extensions.Configuration
open System.Runtime.CompilerServices

[<Sealed>]
[<AbstractClass>]
[<Extension>]
type GoogleLoggerFactoryExtensions =

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member AddGoogle(factory : ILoggerFactory, configuration : IGoogleLoggingConfiguration) =
    let sink = new GoogleSink (configuration) :> ISink
    factory.AddProvider <| new LoggerProvider (sink)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member AddGoogle(factory : ILoggerFactory, configuration : IConfiguration) =
    let config = { ProjectId = null; LogName = null }
    configuration.Bind config
    GoogleLoggerFactoryExtensions.AddGoogle (factory, config :> IGoogleLoggingConfiguration)

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member AddGoogle(factory : ILoggerFactory, projectId, logName) =
    let configuration =
      { ProjectId = projectId
        LogName   = logName }
    GoogleLoggerFactoryExtensions.AddGoogle (factory, configuration)