module NCoreUtils.Logging.Unit.BulkLoopTests

open System
open Microsoft.Extensions.Logging
open NCoreUtils.Logging
open Xunit
open System.Threading

let private category = "category"
let private eventId = EventId (1000, "event")
let private logLevel = LogLevel.Debug

[<Fact>]
let ``single message`` () =
  let entries = ResizeArray ()
  let sink = new TestBulkSink (entries)
  let message = LogMessage<string> (category, logLevel, eventId, null, "message", Func<string, exn, string> (fun msg _ -> msg))
  let messageReceiver =
    [| message |]
    |> Seq.map (fun msg -> msg :> LogMessage)
    |> ResizeArray
    |> TestMessageReceiver
  let loop = BulkLoop (sink, messageReceiver, fun () -> false)
  // run
  loop.Run Unchecked.defaultof<_> |> Async.RunSynchronously
  // check
  Assert.Equal (1, entries.Count)
  Assert.Equal (category, entries.[0].CategoryName)
  Assert.Equal (eventId, entries.[0].EventId)
  Assert.Equal (logLevel, entries.[0].LogLevel)
  Assert.Equal ("message", entries.[0].Message)

[<Fact>]
let ``40 messages`` () =
  let entries = ResizeArray ()
  let sink = new TestBulkSink (entries)
  let messageReceiver =
    [|
      for i = 0 to 39 do
        yield LogMessage<string> (category, logLevel, eventId, null, sprintf "message_%d" i, Func<string, exn, string> (fun msg _ -> msg))
    |]
    |> Seq.map (fun msg -> msg :> LogMessage)
    |> ResizeArray
    |> TestMessageReceiver
  let loop = BulkLoop (sink, messageReceiver, fun () -> false)
  // run
  loop.Run Unchecked.defaultof<_> |> Async.RunSynchronously
  // check
  Assert.Equal (40, entries.Count)
  for i = 0 to 39 do
    Assert.Equal (category, entries.[i].CategoryName)
    Assert.Equal (eventId, entries.[i].EventId)
    Assert.Equal (logLevel, entries.[i].LogLevel)
    Assert.Equal (sprintf "message_%d" i, entries.[i].Message)

[<Fact>]
let ``60 messages`` () =
  let entries = ResizeArray ()
  let sink = new TestBulkSink (entries)
  let messageReceiver =
    [|
      for i = 0 to 59 do
        yield LogMessage<string> (category, logLevel, eventId, null, sprintf "message_%d" i, Func<string, exn, string> (fun msg _ -> msg))
    |]
    |> Seq.map (fun msg -> msg :> LogMessage)
    |> ResizeArray
    |> TestMessageReceiver
  let loop = BulkLoop (sink, messageReceiver, fun () -> false)
  // run
  loop.Run Unchecked.defaultof<_> |> Async.RunSynchronously
  // check
  Assert.Equal (40, entries.Count)
  // run again
  loop.Run Unchecked.defaultof<_> |> Async.RunSynchronously
  // check
  Assert.Equal (60, entries.Count)
  for i = 0 to 59 do
    Assert.Equal (category, entries.[i].CategoryName)
    Assert.Equal (eventId, entries.[i].EventId)
    Assert.Equal (logLevel, entries.[i].LogLevel)
    Assert.Equal (sprintf "message_%d" i, entries.[i].Message)

[<Fact>]
let ``loop cancellation`` () =
  let entries = ResizeArray ()
  let sink = new TestBulkSink (entries)
  let message = LogMessage<string> (category, logLevel, eventId, null, "message", Func<string, exn, string> (fun msg _ -> msg))
  let messageReceiver = InfiniteMessagesWithDelay message
  let loop = BulkLoop (sink, messageReceiver, fun () -> false)
  let cancellationTokenSource = new CancellationTokenSource()
  let computation = loop.Loop Unchecked.defaultof<_>
  let task = Async.StartAsTask (computation, cancellationToken = cancellationTokenSource.Token)
  Thread.Sleep 200
  cancellationTokenSource.Cancel ()
  Thread.Sleep 200
  Assert.True (task.IsCanceled)
