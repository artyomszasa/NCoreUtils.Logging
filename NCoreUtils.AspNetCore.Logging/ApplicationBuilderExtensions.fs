namespace NCoreUtils.Logging

open System.Runtime.CompilerServices
open Microsoft.AspNetCore.Builder
open NCoreUtils.AspNetCore

[<Sealed; AbstractClass>]
[<Extension>]
type ApplicationBuilderNCoreUtilsLoggingExtensions =

  [<Extension>]
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static member UsePrePopulateLoggingContext (applicationBuilder : IApplicationBuilder) =
    applicationBuilder.Use PrePopulateLoggingContextMiddleware.run