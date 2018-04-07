namespace NCoreUtils.Logging

open System
open Microsoft.Extensions.Logging
open NCoreUtils.Logging

[<Struct>]
[<NoEquality; NoComparison>]
type AspNetCoreContext = {
  Method             : string
  Url                : string
  UserAgent          : string
  Referrer           : string
  ResponseStatusCode : Nullable<int>
  RemoteIp           : string
  User               : string }

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
