namespace NCoreUtils.Logging

open Google.Cloud.Logging.Type
open Google.Cloud.Logging.V2
open Google.Protobuf.WellKnownTypes
open Microsoft.Extensions.Logging
open NCoreUtils
open System
open System.Runtime.ExceptionServices
open System.Threading
open System.Runtime.CompilerServices

type IGoogleLoggingConfiguration =
  abstract ProjectId : string
  abstract LogName   : string

[<CLIMutable>]
type GoogleLoggingConfiguration = {
  ProjectId : string
  LogName   : string }
  with
    interface IGoogleLoggingConfiguration with
      member this.ProjectId       = this.ProjectId
      member this.LogName = this.LogName

type [<Sealed>] GoogleSink (configuration : IGoogleLoggingConfiguration) =
  static let mapSeverity =
    let map =
      Map.ofList
        [ LogLevel.Trace,       LogSeverity.Debug
          LogLevel.Debug,       LogSeverity.Debug
          LogLevel.Information, LogSeverity.Info
          LogLevel.Warning,     LogSeverity.Warning
          LogLevel.Error,       LogSeverity.Error
          LogLevel.Critical,    LogSeverity.Critical ]
    fun logLevel -> Map.tryFind logLevel map |? LogSeverity.Default
  static let (|RcpException|_|) = tryGetExn<Grpc.Core.RpcException>
  static let resource = Google.Api.MonitoredResource (Type = "global")
  let client = LoggingServiceV2Client.Create()
  [<VolatileField>]
  let mutable logName = null
  member internal __.GoogleConfiguration = configuration
  member internal __.LogName =
    let mutable currentLogName = logName
    if isNull currentLogName then
      currentLogName <- LogName (configuration.ProjectId, configuration.LogName) |> LogNameOneof.From
      Interlocked.CompareExchange (&logName, currentLogName, null) |> ignore
    currentLogName
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member internal this.CreateLogEntry (timestamp, categoryName, logLevel, eventId : EventId, state : 'state, exn : exn, formatter : Func<_, exn, string>) =
    let optional =
      match exn with
      | null -> ""
      | _    -> sprintf "\n%A" exn
    LogEntry (
      LogName     = this.LogName.ToString (),
      Severity    = mapSeverity logLevel,
      TextPayload = sprintf "[%d] [%s] %s%s" eventId.Id categoryName (formatter.Invoke (state, exn)) optional,
      Timestamp   = Timestamp.FromDateTimeOffset timestamp)
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member internal this.AsyncSend (logEntries : LogEntry[]) = async {
    try
      do! client.AsyncWriteLogEntries (this.LogName, resource, null, logEntries) |> Async.Ignore
    with
      | RcpException rcpExn -> eprintfn "Google cloud logging error: %A" rcpExn.Status
      | exn                 -> ExceptionDispatchInfo.Capture(exn).Throw () }
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member this.AsyncLog (timestamp, categoryName, logLevel, eventId, state, exn, formatter) =
    this.CreateLogEntry (timestamp, categoryName, logLevel, eventId, state, exn, formatter)
    |> Array.singleton
    |> this.AsyncSend
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member this.CreateQueue () = new GoogleSinkQueue (this)
  interface IDisposable with
    member __.Dispose () = ()
  interface ISink with
    member this.AsyncLog (timestamp, categoryName, logLevel, eventId, state, exn, formatter) =
      this.AsyncLog (timestamp, categoryName, logLevel, eventId, state, exn, formatter)
  interface IBulkSink with
    member this.CreateQueue () = this.CreateQueue () :> _

and GoogleSinkQueue (sink : GoogleSink) =
  let entries = ResizeArray ()
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member private __.AsyncFlush () =
    let toSend = entries.ToArray ()
    entries.Clear ()
    sink.AsyncSend toSend
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member this.Dispose () =
    if 0 <> entries.Count then
      this.AsyncFlush ()
      |> Async.Start
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member __.Enqueue (timestamp, categoryName, logLevel, eventId, state, exn, formatter) =
    entries.Add <| sink.CreateLogEntry (timestamp, categoryName, logLevel, eventId, state, exn, formatter)
  interface ISinkQueue with
    member this.Enqueue (timestamp, categoryName, logLevel, eventId, state, exn, formatter) =
      this.Enqueue (timestamp, categoryName, logLevel, eventId, state, exn, formatter)
    member this.AsyncFlush () = this.AsyncFlush ()
    member this.Dispose () = this.Dispose ()