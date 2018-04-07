namespace NCoreUtils.Logging

open Microsoft.Extensions.Logging
open NCoreUtils
open System
open System.Threading
open System.Threading.Tasks

type ISink =
  inherit IDisposable
  abstract AsyncLog<'state> : timestamp:DateTimeOffset * categoryName:string * logLevel:LogLevel * eventId:EventId * state:'state * exn:exn * formatter:Func<'state, exn, string> -> Async<unit>

type ISinkQueue =
  inherit IDisposable
  abstract Enqueue<'state> : timestamp:DateTimeOffset * categoryName:string * logLevel:LogLevel * eventId:EventId * state:'state * exn:exn * formatter:Func<'state, exn, string> -> unit
  abstract AsyncFlush : unit -> Async<unit>

type IBulkSink =
  inherit ISink
  abstract CreateQueue : unit -> ISinkQueue

// C# interop

[<AbstractClass>]
type AsyncSink () =
  abstract LogAsync<'state>
    :  timestamp:DateTimeOffset
    *  categoryName:string
    *  logLevel:LogLevel
    *  eventId:EventId
    *  state:'state
    *  exn:exn
    *  formatter:Func<'state, exn, string>
    *  cancellationToken:CancellationToken
    -> Task
  abstract Dispose : unit -> unit
  interface IDisposable with
    member this.Dispose () = this.Dispose ()
  interface ISink with
    member this.AsyncLog<'state> (timestamp, categoryName, logLevel, eventId, state, exn, formatter) =
      Async.Adapt (fun cancellationToken -> this.LogAsync<'state> (timestamp, categoryName, logLevel, eventId, state, exn, formatter, cancellationToken))
