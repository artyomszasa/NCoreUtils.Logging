namespace NCoreUtils.Logging

open System.Runtime.CompilerServices
open Microsoft.Extensions.DependencyInjection.Extensions
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection

[<Extension>]
[<Sealed; AbstractClass>]
type LoggingBuilderExtensions =

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member AddSink<'sink when 'sink :> ISink and 'sink : not struct> (builder : ILoggingBuilder) =
    builder.Services.TryAddSingleton<'sink> ()
    builder.Services.TryAddEnumerable (ServiceDescriptor.Singleton<ILoggerProvider, AspNetCoreLoggerProvider<'sink>> ())
    builder