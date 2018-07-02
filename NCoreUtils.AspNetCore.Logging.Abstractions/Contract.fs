namespace NCoreUtils.Logging

open System
open Microsoft.Extensions.Logging
open NCoreUtils.Logging

/// Represents actual ASP.NET Core context.
[<Struct>]
[<NoEquality; NoComparison>]
type AspNetCoreContext = {
  /// Request method.
  Method             : string
  /// Requested URL.
  Url                : string
  /// User agent string.
  UserAgent          : string
  /// Referrer.
  Referrer           : string
  /// Response status code.
  ResponseStatusCode : Nullable<int>
  /// Remote ip address.
  RemoteIp           : string
  /// User if present.
  User               : string }

/// Defines functionality to log messages with ASP.NET Core context.
type IAspNetCoreSink =
  inherit ISink
  abstract AsyncLog<'state>
    :  timestamp:DateTimeOffset
    *  categoryName:string
    *  logLevel:LogLevel
    *  eventId:EventId
    *  state:'state
    *  exn:exn
    *  formatter:Func<'state, exn, string>
    *  context:AspNetCoreContext
    -> Async<unit>

/// Defines functionality to queue messages with ASP.NET Core context.
type IAspNetCoreSinkQueue =
  inherit ISinkQueue
  abstract Enqueue<'state>
    :  timestamp:DateTimeOffset
    *  categoryName:string
    *  logLevel:LogLevel
    *  eventId:EventId
    *  state:'state
    *  exn:exn
    *  formatter:Func<'state, exn, string>
    *  context:AspNetCoreContext
    -> unit
