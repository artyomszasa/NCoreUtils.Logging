module NCoreUtils.Logging.Unit.LogMessageTests

open System
open Microsoft.Extensions.Logging
open Xunit
open NCoreUtils.Logging

type AssertSink<'a> (expectedCategory, expectedLogLevel, expectedEventId, expectedExn, expectedState : 'a, expectedFormatter : Func<'a, exn, string>) =
  interface ISink with
    member __.AsyncLog(_: DateTimeOffset, categoryName: string, logLevel: LogLevel, eventId: EventId, state: 'state, exn: exn, formatter: Func<'state,exn,string>) =
      Assert.Equal (expectedCategory, categoryName)
      Assert.Equal (expectedLogLevel, logLevel)
      Assert.Equal (expectedEventId, eventId)
      Assert.Equal (expectedState.GetType (), state.GetType ())
      Assert.Equal (expectedState, unbox<'a> (box state))
      Assert.Equal (expectedExn, exn)
      Assert.Same (box expectedFormatter, box formatter)
      async.Zero ()
    member __.Dispose () =  ()

type AssertQueue<'a> (expectedCategory, expectedLogLevel, expectedEventId, expectedExn, expectedState : 'a, expectedFormatter : Func<'a, exn, string>) =
  interface ISinkQueue with
    member __.AsyncFlush () = async.Zero ()
    member __.Dispose    () = ()
    member __.Enqueue(_: DateTimeOffset, categoryName: string, logLevel: LogLevel, eventId: EventId, state: 'state, exn: exn, formatter: Func<'state,exn,string>) =
      Assert.Equal (expectedCategory, categoryName)
      Assert.Equal (expectedLogLevel, logLevel)
      Assert.Equal (expectedEventId, eventId)
      Assert.Equal (expectedState.GetType (), state.GetType ())
      Assert.Equal (expectedState, unbox<'a> (box state))
      Assert.Equal (expectedExn, exn)
      Assert.Same (box expectedFormatter, box formatter)


[<Fact>]
let enqueue () =
  let category = "test category"
  let logLevel = LogLevel.Error
  let eventId = EventId (100, "some event")
  let exn = InvalidOperationException ()
  let state = 2
  let formatter = Func<int, exn, string> (fun i _ -> string i)
  let message = LogMessage<int>(category, logLevel, eventId, exn, state, formatter)
  let assertSink = new AssertSink<int>(category, logLevel, eventId, exn, state, formatter)
  let assertQueue = new AssertQueue<int>(category, logLevel, eventId, exn, state, formatter)
  message.AsyncLog assertSink |> Async.RunSynchronously
  message.Enqueue assertQueue
