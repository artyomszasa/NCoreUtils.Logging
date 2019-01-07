namespace NCoreUtils.Logging

open NCoreUtils
open NCoreUtils.Logging
open Microsoft.AspNetCore.Http
open System
open System.Linq.Expressions
open System.Threading

[<AutoOpen>]
module private Helpers =

  let passString =
    let stringArg = Expression.Parameter typeof<string>
    let exnArg    = Expression.Parameter typeof<exn>
    let expr      =
      Expression.Lambda<Func<string, exn, string>> (
        stringArg,
        [| stringArg; exnArg |])
    expr.Compile ()

  [<RequiresExplicitTypeArguments>]
  let inline tryGetServiceSafe<'a> sp =
    match sp with
    | null -> None
    | _    -> tryGetService<'a> sp

type AspNetCoreLogger (provider : LoggerProvider, category, httpContextAccessor : IHttpContextAccessor) =
  inherit Logger (provider, category)

  override __.Log (logLevel, eventId, state : 'state, exn, formatter) =
    let context =
      try
        match httpContextAccessor.HttpContext with
        | null -> Unchecked.defaultof<_>
        | httpContext ->
          match tryGetServiceSafe<LoggingContext> httpContext.RequestServices with
          | Some loggingContext ->
            // Current user may have changed during the execution, try update
            try
              match httpContext.User with
              | null -> ()
              | principal when principal.Identity.IsAuthenticated ->
                loggingContext.AspNetCoreContext <- { loggingContext.AspNetCoreContext with User = principal.Identity.Name }
              | _ -> ()
            with _ -> ()
            loggingContext.AspNetCoreContext
          | _                   ->
            let mutable lockTaken = Unchecked.defaultof<_>
            Monitor.Enter (httpContext, &lockTaken)
            try
              PrePopulateLoggingContextMiddleware.populateContext httpContext
            finally
              if lockTaken then Monitor.Exit httpContext
      with _ ->
        Unchecked.defaultof<_>
    // State may contain non-threadsafe references (e.g. Microsoft.AspNetCore.Hosting.Internal.HostingRequestStartingLog -> HttpContext)
    // thus formatter should be executed before passing to the delivery thread
    let newState = formatter.Invoke (state, exn)
    provider.PushMessage <| AspNetCoreLogMessage (category, logLevel, eventId, exn, newState, passString, context)

type AspNetCoreLoggerProvider (sink, httpContextAccessor) =
  inherit LoggerProvider (sink)
  override this.CreateLogger category = AspNetCoreLogger (this, category, httpContextAccessor) :> _

type AspNetCoreLoggerProvider<'sink when 'sink :> ISink> (sink : 'sink, httpContextAccessor) =
  inherit AspNetCoreLoggerProvider (sink :> ISink, httpContextAccessor)
