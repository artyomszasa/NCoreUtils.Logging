namespace NCoreUtils.Logging

open NCoreUtils
open NCoreUtils.Logging
open Microsoft.AspNetCore.Http
open System.Threading

[<AutoOpen>]
module private Helpers =

  [<RequiresExplicitTypeArguments>]
  let inline tryGetServiceSafe<'a> sp =
    match sp with
    | null -> None
    | _    -> tryGetService<'a> sp

type AspNetCoreLogger (provider : LoggerProvider, category, httpContextAccessor : IHttpContextAccessor) =
  inherit Logger (provider, category)

  override __.Log (logLevel, eventId, state : 'state, exn, formatter) =
    let context =
      match httpContextAccessor.HttpContext with
      | null -> Unchecked.defaultof<_>
      | httpContext ->
        match tryGetServiceSafe<LoggingContext> httpContext.RequestServices with
        | Some loggingContext -> loggingContext.AspNetCoreContext
        | _                   ->
          let mutable lockTaken = Unchecked.defaultof<_>
          Monitor.Enter (httpContext, &lockTaken)
          try
            PrePopulateLoggingContextMiddleware.populateContext httpContext
          finally
            if lockTaken then Monitor.Exit httpContext
    provider.PushMessage <| AspNetCoreLogMessage<'state> (category, logLevel, eventId, exn, state, formatter, context)

type AspNetCoreLoggerProvider (sink, httpContextAccessor) =
  inherit LoggerProvider (sink)
  override this.CreateLogger category = AspNetCoreLogger (this, category, httpContextAccessor) :> _

type AspNetCoreLoggerProvider<'sink when 'sink :> ISink> (sink : 'sink, httpContextAccessor) =
  inherit AspNetCoreLoggerProvider (sink :> ISink, httpContextAccessor)
