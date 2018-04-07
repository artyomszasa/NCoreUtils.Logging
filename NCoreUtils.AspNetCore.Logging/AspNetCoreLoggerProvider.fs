namespace NCoreUtils.Logging

open NCoreUtils
open NCoreUtils.Logging
open Microsoft.AspNetCore.Http
open Microsoft.FSharp.Linq
open System
open System.Security.Claims

type AspNetCoreLogger (provider : LoggerProvider, category, httpContextAccessor : IHttpContextAccessor) =
  inherit Logger (provider, category)

  static let claimValue (claim : Claim) = claim.Value

  override __.Log (logLevel, eventId, state : 'state, exn, formatter) =
    let context =
      match httpContextAccessor.HttpContext with
      | null -> Unchecked.defaultof<_>
      | httpContext ->
        let request = httpContext.Request
        let uri =
          let host =
            match request.Host.HasValue with
            | true -> request.Host.Host
            | _    -> "localhost"
          let port =
            match request.Host.HasValue && request.Host.Port.HasValue with
            | true ->
              let port = request.Host.Port.Value
              match (request.IsHttps && port = 443) || (not request.IsHttps && port = 80) with
              | true -> -1
              | _    -> port
            | _ -> -1
          let builder =
            UriBuilder (
              Scheme = request.Scheme,
              Host = host,
              Port = port,
              Path = request.Path.Value,
              Query = request.QueryString.ToUriComponent())
          builder.Uri
        let userAgent =
          let mutable values = Unchecked.defaultof<_>
          match request.Headers.TryGetValue ("User-Agent", &values) with
          | true when 0 < values.Count -> values.[0]
          | _ -> "unknown"
        let referrer =
          let mutable values = Unchecked.defaultof<_>
          match request.Headers.TryGetValue ("Referer", &values) with
          | true when 0 < values.Count -> values.[0]
          | _ -> null
        let user =
          match httpContext.User with
          | null -> null
          | user -> user.FindFirst ClaimTypes.Name |> Option.wrap >>| claimValue |> Option.getOrUndef
        { Method             = httpContext.Request.Method
          Url                = uri.AbsoluteUri
          UserAgent          = userAgent
          Referrer           = referrer
          ResponseStatusCode = Nullable.mk httpContext.Response.StatusCode
          RemoteIp           = httpContext.Connection.RemoteIpAddress.ToString ()
          User               = user }
    provider.PushMessage <| AspNetCoreLogMessage<'state> (category, logLevel, eventId, exn, state, formatter, context)

type AspNetCoreLoggerProvider (sink, httpContextAccessor) =
  inherit LoggerProvider (sink)
  override this.CreateLogger category = AspNetCoreLogger (this, category, httpContextAccessor) :> _

type AspNetCoreLoggerProvider<'sink when 'sink :> ISink> (sink : 'sink, httpContextAccessor) =
  inherit AspNetCoreLoggerProvider (sink :> ISink, httpContextAccessor)
