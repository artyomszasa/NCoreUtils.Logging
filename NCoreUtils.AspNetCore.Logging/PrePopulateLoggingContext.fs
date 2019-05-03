namespace NCoreUtils.Logging

open System
open System.Collections.Immutable
open System.Diagnostics.CodeAnalysis
open System.Security.Claims
open NCoreUtils
open Microsoft.AspNetCore.Http

type internal LoggingContext () =
  member val AspNetCoreContext = Unchecked.defaultof<AspNetCoreContext> with get, set

[<RequireQualifiedAccess>]
module PrePopulateLoggingContextMiddleware =

  [<ExcludeFromCodeCoverage>]
  let inline private getEffectiveHost (host : HostString) =
    match host.HasValue with
    | true -> host.Host
    | _    -> "localhost"

  [<ExcludeFromCodeCoverage>]
  let inline private getEffectivePort (request : HttpRequest) =
    match request.Host.HasValue && request.Host.Port.HasValue with
    | true ->
      let port = request.Host.Port.Value
      match (request.IsHttps && port = 443) || (not request.IsHttps && port = 80) with
      | true -> -1
      | _    -> port
    | _ -> -1

  [<ExcludeFromCodeCoverage>]
  let inline private getUserAgentString (headers : IHeaderDictionary) =
    let mutable values = Unchecked.defaultof<_>
    match headers.TryGetValue ("User-Agent", &values) with
    | true when 0 < values.Count -> values.[0]
    | _                          -> "unknown"

  [<ExcludeFromCodeCoverage>]
  let inline private getReferrer (headers : IHeaderDictionary) =
    let mutable values = Unchecked.defaultof<_>
    match headers.TryGetValue ("Referer", &values) with
    | true when 0 < values.Count -> values.[0]
    | _ -> null

  [<ExcludeFromCodeCoverage>]
  let inline private getUser (httpContext : HttpContext) =
    match httpContext.User with
    | null -> null
    | user -> user.Identity.Name

  [<CompiledName("PopulateContext")>]
  let internal populateContext (httpContext : HttpContext) =
    let request = httpContext.Request
    let uri =
      let builder =
        UriBuilder (
          Scheme = request.Scheme,
          Host = getEffectiveHost request.Host,
          Port = getEffectivePort request,
          Path = request.Path.ToUriComponent (),
          Query = request.QueryString.ToUriComponent ())
      builder.Uri
    let userAgent    = getUserAgentString request.Headers
    let referrer     = getReferrer request.Headers
    let user         = getUser httpContext
    let headers      = { Instance = ImmutableDictionary.CreateRange (StringComparer.OrdinalIgnoreCase, request.Headers) }
    let connectionId =
      match httpContext.Connection with
      | null       -> null
      | connection -> connection.Id
    let remoteIpAddress =
      match httpContext.Connection with
      | null       -> "<none>"
      | connection ->
      match connection.RemoteIpAddress with
      | null            -> "<none>"
      | remoteIpAddress -> remoteIpAddress.ToString ()
    { ConnectionId       = connectionId
      Method             = httpContext.Request.Method
      Url                = uri.AbsoluteUri
      UserAgent          = userAgent
      Referrer           = referrer
      ResponseStatusCode = Nullable.mk httpContext.Response.StatusCode
      RemoteIp           = remoteIpAddress
      Headers            = headers
      User               = user }

  [<CompiledName("Run")>]
  let run (httpContext : HttpContext) (asyncNext : Async<unit>) =
    match tryGetService<LoggingContext> httpContext.RequestServices with
    | Some loggingContext ->
      loggingContext.AspNetCoreContext <- populateContext httpContext
    | _ -> ()
    asyncNext