namespace NCoreUtils.Logging

open System.Runtime.CompilerServices
open Microsoft.Extensions.DependencyInjection

[<Sealed; AbstractClass>]
[<Extension>]
type ServiceCollectionNCoreUtilsAspNetCoreLoggingExtensions =

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member AddPrePopulatedLoggingContext (services : IServiceCollection) =
    services.AddScoped<LoggingContext> ()