namespace NCoreUtils.Logging.Unit

open System
open Microsoft.Extensions.Logging
open NCoreUtils.Logging

type Entry = {
  Timestamp    : DateTimeOffset
  CategoryName : string
  LogLevel     : LogLevel
  EventId      : EventId
  Message      : string }

type AspEntry = {
  Timestamp    : DateTimeOffset
  CategoryName : string
  LogLevel     : LogLevel
  EventId      : EventId
  Message      : string
  Context      : AspNetCoreContext }

type TestSinkQueue (target : ResizeArray<_>) =
  let mutable disposed = false
  let pending = ResizeArray<_> ()
  member __.Disposed = disposed
  interface ISinkQueue with
    member __.AsyncFlush () =
      target.AddRange pending
      pending.Clear ()
      async.Zero ()
    member __.Dispose () =
      disposed <- true
    member __.Enqueue (timestamp, categoryName, logLevel, eventId, state: 'state, exn: exn, formatter: System.Func<'state,exn,string>) =
      pending.Add
        { Timestamp    = timestamp
          CategoryName = categoryName
          LogLevel     = logLevel
          EventId      = eventId
          Message      = formatter.Invoke (state, exn) }

type TestBulkSink (target : ResizeArray<_>) =
  let mutable disposed = false
  member __.Disposed = disposed
  interface ISink with
    member __.AsyncLog (timestamp, categoryName, logLevel, eventId, state: 'state, exn: exn, formatter: Func<'state,exn,string>) =
      target.Add
        { Timestamp    = timestamp
          CategoryName = categoryName
          LogLevel     = logLevel
          EventId      = eventId
          Message      = formatter.Invoke (state, exn) }
      async.Zero ()
    member __.Dispose () =
      disposed <- true
  interface IBulkSink with
    member __.CreateQueue() =
      new TestSinkQueue (target) :> _

type TestSink (target : ResizeArray<_>) =
  let mutable disposed = false
  member __.Disposed = disposed
  interface ISink with
    member __.AsyncLog (timestamp, categoryName, logLevel, eventId, state: 'state, exn: exn, formatter: Func<'state,exn,string>) =
      target.Add
        { Timestamp    = timestamp
          CategoryName = categoryName
          LogLevel     = logLevel
          EventId      = eventId
          Message      = formatter.Invoke (state, exn) }
      async.Zero ()
    member __.Dispose () =
      disposed <- true

type TestAspSinkQueue (target : ResizeArray<_>) =
  let mutable disposed = false
  let pending = ResizeArray<_> ()
  member __.Disposed = disposed
  interface ISinkQueue with
    member __.AsyncFlush () =
      target.AddRange pending
      pending.Clear ()
      async.Zero ()
    member __.Dispose () =
      disposed <- true
    member __.Enqueue (timestamp, categoryName, logLevel, eventId, state: 'state, exn: exn, formatter: System.Func<'state,exn,string>) =
      pending.Add
        { Timestamp    = timestamp
          CategoryName = categoryName
          LogLevel     = logLevel
          EventId      = eventId
          Message      = formatter.Invoke (state, exn)
          Context      = AspNetCoreContext.empty }
  interface IAspNetCoreSinkQueue with
    member __.Enqueue (timestamp, categoryName, logLevel, eventId, state: 'state, exn: exn, formatter: System.Func<'state,exn,string>, context) =
      pending.Add
        { Timestamp    = timestamp
          CategoryName = categoryName
          LogLevel     = logLevel
          EventId      = eventId
          Message      = formatter.Invoke (state, exn)
          Context      = context }

type TestAspBulkSink (target : ResizeArray<_>) =
  let mutable disposed = false
  member __.Disposed = disposed
  interface ISink with
    member __.AsyncLog (timestamp, categoryName, logLevel, eventId, state: 'state, exn: exn, formatter: Func<'state,exn,string>) =
      target.Add
        { Timestamp    = timestamp
          CategoryName = categoryName
          LogLevel     = logLevel
          EventId      = eventId
          Message      = formatter.Invoke (state, exn)
          Context      = AspNetCoreContext.empty }
      async.Zero ()
    member __.Dispose () =
      disposed <- true
  interface IBulkSink with
    member __.CreateQueue() =
      new TestAspSinkQueue (target) :> _
  interface IAspNetCoreSink with
    member __.AsyncLog (timestamp, categoryName, logLevel, eventId, state: 'state, exn: exn, formatter: Func<'state,exn,string>, context) =
      target.Add
        { Timestamp    = timestamp
          CategoryName = categoryName
          LogLevel     = logLevel
          EventId      = eventId
          Message      = formatter.Invoke (state, exn)
          Context      = context }
      async.Zero ()

type TestAspSink (target : ResizeArray<_>) =
  let mutable disposed = false
  member __.Disposed = disposed
  interface ISink with
    member __.AsyncLog (timestamp, categoryName, logLevel, eventId, state: 'state, exn: exn, formatter: Func<'state,exn,string>) =
      target.Add
        { Timestamp    = timestamp
          CategoryName = categoryName
          LogLevel     = logLevel
          EventId      = eventId
          Message      = formatter.Invoke (state, exn)
          Context      = AspNetCoreContext.empty }
      async.Zero ()
    member __.Dispose () =
      disposed <- true
  interface IAspNetCoreSink with
    member __.AsyncLog (timestamp, categoryName, logLevel, eventId, state: 'state, exn: exn, formatter: Func<'state,exn,string>, context) =
      target.Add
        { Timestamp    = timestamp
          CategoryName = categoryName
          LogLevel     = logLevel
          EventId      = eventId
          Message      = formatter.Invoke (state, exn)
          Context      = context }
      async.Zero ()



type NullSinkQueue () =
  interface ISinkQueue with
    member __.AsyncFlush () = async.Zero ()
    member __.Dispose () = ()
    member __.Enqueue (_, _, _, _, _, _, _) = ()

type NullBulkSink () =
  interface ISink with
    member __.AsyncLog (_, _, _, _, _, _, _) = async.Zero ()
    member __.Dispose () = ()
  interface IBulkSink with
    member __.CreateQueue () = new NullSinkQueue () :> _


type ErrorSinkQueue () =
  interface ISinkQueue with
    member __.AsyncFlush () = async { failwith "test error" }
    member __.Dispose () = ()
    member __.Enqueue (_, _, _, _, _, _, _) = ()

type ErrorBulkSink () =
  interface ISink with
    member __.AsyncLog (_, _, _, _, _, _, _) = async { failwith "test error" }
    member __.Dispose () = ()
  interface IBulkSink with
    member __.CreateQueue () = new ErrorSinkQueue () :> _

type ErrorSink () =
  interface ISink with
    member __.AsyncLog (_, _, _, _, _, _, _) = async { failwith "test error" }
    member __.Dispose () = ()
