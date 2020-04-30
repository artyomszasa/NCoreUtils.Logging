namespace NCoreUtils.Logging

open System
open System.Runtime.CompilerServices
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.DependencyInjection.Extensions
open Microsoft.Extensions.Logging

type Configure = Action<GoogleLoggingConfiguration>

[<Extension>]
[<Sealed; AbstractClass>]
type LoggingBuilderGoogleExtensions =

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member private AddSink<'sink when 'sink :> ISink and 'sink : not struct> (builder : ILoggingBuilder) =
    builder.Services.AddSingleton<ISink, 'sink>() |> ignore
    let svc = ServiceDescriptor.Singleton<ILoggerProvider, LoggerProvider> ()
    builder.Services.TryAddEnumerable svc
    builder

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member AddGoogleSink (builder : ILoggingBuilder, config : GoogleLoggingConfiguration) =
    builder.Services.AddSingleton config |> ignore
    builder.AddSink<GoogleSink> ()

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member AddGoogleSink (builder : ILoggingBuilder, configuration : IConfiguration) =
    let config = { ProjectId = null; LogName = null }
    configuration.Bind config
    LoggingBuilderGoogleExtensions.AddGoogleSink (builder, config)

