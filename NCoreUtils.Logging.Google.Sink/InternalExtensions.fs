namespace NCoreUtils.Logging

open Google.Cloud.Logging.V2
open NCoreUtils
open System.Runtime.CompilerServices

[<AutoOpen>]
module internal InternalExtensions =

  type LoggingServiceV2Client with
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member this.AsyncWriteLogEntries (logName, resource, labels, logEntries) =
      Async.Adapt (fun _ -> this.WriteLogEntriesAsync (logName, resource, labels, logEntries))