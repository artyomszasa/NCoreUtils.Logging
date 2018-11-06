namespace NCoreUtils.Logging

open System
open System.Security.Claims
open NCoreUtils
open Microsoft.AspNetCore.Http
open System.Collections.Immutable

type internal LoggingContext () =
  member val AspNetCoreContext = Unchecked.defaultof<AspNetCoreContext> with get, set

[<RequireQualifiedAccess>]
module PrePopulateLoggingContextMiddleware =

  let inline private getEffectiveHost (host : HostString) =
    match host.HasValue with
    | true -> host.Host
    | _    -> "localhost"

  let inline private getEffectivePort (request : HttpRequest) =
    match request.Host.HasValue && request.Host.Port.HasValue with
    | true ->
      let port = request.Host.Port.Value
      match (request.IsHttps && port = 443) || (not request.IsHttps && port = 80) with
      | true -> -1
      | _    -> port
    | _ -> -1

  let inline private getUserAgentString (headers : IHeaderDictionary) =
    let mutable values = Unchecked.defaultof<_>
    match headers.TryGetValue ("User-Agent", &values) with
    | true when 0 < values.Count -> values.[0]
    | _                          -> "unknown"

  let inline private getReferrer (headers : IHeaderDictionary) =
    let mutable values = Unchecked.defaultof<_>
    match headers.TryGetValue ("Referer", &values) with
    | true when 0 < values.Count -> values.[0]
    | _ -> null

  let inline private claimValue (claim : Claim) =
    match claim with
    | null  -> null
    | _     -> claim.Value

  let inline private getUser (httpContext : HttpContext) =
    match httpContext.User with
    | null -> null
    | user -> user.FindFirst ClaimTypes.Name |> claimValue

  [<CompiledName("PopulateContext")>]
  let internal populateContext (httpContext : HttpContext) =
    let request = httpContext.Request
    let uri =
      let builder =
        UriBuilder (
          Scheme = request.Scheme,
          Host = getEffectiveHost request.Host,
          Port = getEffectivePort request,
          Path = request.Path.Value,
          Query = request.QueryString.ToUriComponent())
      builder.Uri
    let userAgent  = getUserAgentString request.Headers
    let referrer   = getReferrer request.Headers
    let user       = getUser httpContext
    let headers    = ImmutableDictionary.CreateRange (StringComparer.OrdinalIgnoreCase, request.Headers)
    let httpMethod = httpContext.Request.Method
    let statusCode =
      match httpContext.Response with
      | null     -> Nullable.empty
      | response -> Nullable.mk response.StatusCode
    let remoteIp =
      match httpContext.Connection with
      | null -> "<null>"
      | connection ->
        match connection.RemoteIpAddress with
        | null -> "<null>"
        | addr -> addr.ToString ()
    { Method             = httpMethod
      Url                = uri.AbsoluteUri
      UserAgent          = userAgent
      Referrer           = referrer
      ResponseStatusCode = statusCode
      RemoteIp           = remoteIp
      Headers            = headers
      User               = user }

  [<CompiledName("Run")>]
  let run (httpContext : HttpContext) (asyncNext : Async<unit>) =
    match tryGetService<LoggingContext> httpContext.RequestServices with
    | Some loggingContext ->
      loggingContext.AspNetCoreContext <- populateContext httpContext
    | _ -> ()
    asyncNext