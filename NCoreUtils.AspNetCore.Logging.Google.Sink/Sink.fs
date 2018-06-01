namespace NCoreUtils.Logging.Google

#nowarn "64"

open System
open System.Runtime.CompilerServices
open System.Runtime.ExceptionServices
open System.Threading
open Google.Cloud.Logging.Type
open Google.Cloud.Logging.V2
open Google.Protobuf.WellKnownTypes
open Microsoft.Extensions.Logging
open NCoreUtils
open NCoreUtils.Logging
open Newtonsoft.Json

[<Sealed>]
type private JsonDateTimeOffsetConverter () =
  inherit JsonConverter ()
  override __.CanRead = false
  override __.CanConvert ``type`` = ``type`` = typeof<DateTimeOffset>
  override __.WriteJson (writer, value, _) =
    match value with
    | null -> writer.WriteNull ()
    | :? DateTimeOffset as v -> v.ToString("o") |> writer.WriteValue
    | _ -> invalidOpf "should never happen"
  override __.ReadJson (_, _, _, _) = notImplemented "Should never be called."


// [<CLIMutable>]
// type JsonPayloadServiceContext = {
//   [<JsonProperty("service", Required = Required.Always)>]
//   Service : string
//   [<JsonProperty("version", Required = Required.DisallowNull)>]
//   Version : string }
//
// [<CLIMutable>]
// type JsonPayload = {
//   [<JsonProperty("eventTime", Required = Required.Always)>]
//   [<JsonConverter(typeof<JsonDateTimeOffsetConverter>)>]
//   EventTime      : DateTimeOffset
//   [<JsonProperty("serviceContext", Required = Required.Always)>]
//   ServiceContext : JsonPayloadServiceContext
//   [<JsonProperty("message", Required = Required.Always)>]
//   Message        : string
//   [<JsonProperty("context", Required = Required.DisallowNull)>]
//   Context        : JsonPayloadContext }

[<AutoOpen>]
module JsonPayload =

  [<AbstractClass; Sealed>]
  type ToValue =
    static member inline ToValue (_ : ToValue, string : string) = Value.ForString string
    static member inline ToValue (_ : ToValue, num : int) = Value.ForNumber (float num)
    static member inline ToValue (_ : ToValue, s : Struct) = Value.ForStruct s

  let inline private toValue (value : ^a) : Value
    when ^x :> ToValue
    and  (^a or ^x) : (static member ToValue : ^x * ^a -> Value)
    = ((^a or ^x) : (static member ToValue : ^x * ^a -> Value) (Unchecked.defaultof<ToValue>, value))

  let inline private add key value (obj : Struct) =
    obj.Fields.Add (key, toValue value)
    obj

  let inline private addNotNull key value (obj : Struct) =
    if not (String.IsNullOrEmpty value) then
      obj.Fields.Add (key, toValue value)
    obj

  let inline private addHasValue key (value : Nullable<_>) (obj : Struct) =
    match value.HasValue with
    | true -> obj.Fields.Add (key, toValue value.Value)
    | _    -> ()
    obj

  type AspNetCoreContext with
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member internal this.ToStruct () =
      Struct ()
      |> add         "method"             this.Method
      |> add         "url"                this.Url
      |> add         "userAgent"          this.UserAgent
      |> addNotNull  "referrer"           this.Referrer
      |> addHasValue "responseStatusCode" this.ResponseStatusCode
      |> add         "remoteIp"           this.RemoteIp
      |> addNotNull  "user"               this.User

  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  let internal mkServiceContext (name : string) version =
    Struct ()
    |> add        "service" name
    |> addNotNull "version" version

  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  let internal mkJsonPayload (eventTime : DateTimeOffset) serviceName serviceVersion message (context : AspNetCoreContext) =
    Struct ()
    |> add "eventTime"      (eventTime.ToString "o")
    |> add "serviceContext" (mkServiceContext serviceName serviceVersion)
    |> add "message"         message
    |> add "context"        (context.ToStruct ())

type IGoogleAspNetCoreLoggingConfiguration =
  abstract ProjectId      : string
  abstract ServiceName    : string
  abstract ServiceVersion : string

[<CLIMutable>]
type GoogleAspNetCoreLoggingConfiguration = {
  ProjectId      : string
  ServiceName    : string
  ServiceVersion : string }
  with
    interface IGoogleAspNetCoreLoggingConfiguration with
      member this.ProjectId      = this.ProjectId
      member this.ServiceName    = this.ServiceName
      member this.ServiceVersion = this.ServiceVersion

type [<Sealed>] GoogleAspNetCoreSink (configuration : IGoogleAspNetCoreLoggingConfiguration) =
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
  // let mutable serviceContext = { Service = configuration.ServiceName; Version = configuration.ServiceVersion }
  member internal __.GoogleConfiguration = configuration
  member internal __.LogName =
    let mutable currentLogName = logName
    if isNull currentLogName then
      let serviceName =
        match String.IsNullOrEmpty configuration.ServiceVersion with
        | true -> configuration.ServiceName
        | _    -> sprintf "%s-%s" configuration.ServiceName configuration.ServiceVersion
      currentLogName <- LogName (configuration.ProjectId, serviceName) |> LogNameOneof.From
      Interlocked.CompareExchange (&logName, currentLogName, null) |> ignore
    currentLogName
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member internal this.CreateLogEntry (timestamp : DateTimeOffset, categoryName, logLevel, eventId : EventId, state : 'state, exn : exn, formatter : Func<_, exn, string>, context : AspNetCoreContext) =
    let message =
      let optional =
        match exn with
        | null -> ""
        | _    -> sprintf "\n%A" exn
      sprintf "[%d] [%s] %s%s" eventId.Id categoryName (formatter.Invoke (state, exn)) optional
    let jsonPayload =
      match struct (exn, box context) with
      | struct (null, _)
      | struct (_, null) -> null
      | _ -> mkJsonPayload timestamp configuration.ServiceName configuration.ServiceVersion message context
    let entry =
      LogEntry (
        LogName     = this.LogName.ToString (),
        Severity    = mapSeverity logLevel,
        Timestamp   = Timestamp.FromDateTimeOffset timestamp)
    match jsonPayload with
    | null -> entry.TextPayload <- message
    | _    -> entry.JsonPayload <- jsonPayload
    entry
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member internal this.AsyncSend (logEntries : LogEntry[]) = async {
    try
      do! client.AsyncWriteLogEntries (this.LogName, resource, null, logEntries) |> Async.Ignore
    with
      | RcpException rcpExn -> eprintfn "Google cloud logging error: %A" rcpExn.Status
      | exn                 -> ExceptionDispatchInfo.Capture(exn).Throw () }
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member this.AsyncLog (timestamp, categoryName, logLevel, eventId, state, exn, formatter, context) =
    this.CreateLogEntry (timestamp, categoryName, logLevel, eventId, state, exn, formatter, context)
    |> Array.singleton
    |> this.AsyncSend
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member this.CreateQueue () = new GoogleSinkQueue (this)
  interface IDisposable with
    member __.Dispose () = ()
  interface ISink with
    member this.AsyncLog (timestamp, categoryName, logLevel, eventId, state, exn, formatter) =
      this.AsyncLog (timestamp, categoryName, logLevel, eventId, state, exn, formatter, Unchecked.defaultof<_>)
  interface IAspNetCoreSink with
    member this.AsyncLog (timestamp, categoryName, logLevel, eventId, state, exn, formatter, context) =
      this.AsyncLog (timestamp, categoryName, logLevel, eventId, state, exn, formatter, context)
  interface IBulkSink with
    member this.CreateQueue () = this.CreateQueue () :> _

and GoogleSinkQueue (sink : GoogleAspNetCoreSink) =
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
  member __.Enqueue (timestamp, categoryName, logLevel, eventId, state, exn, formatter, context) =
    entries.Add <| sink.CreateLogEntry (timestamp, categoryName, logLevel, eventId, state, exn, formatter, context)
  interface IAspNetCoreSinkQueue with
    member this.Enqueue (timestamp, categoryName, logLevel, eventId, state, exn, formatter) =
      this.Enqueue (timestamp, categoryName, logLevel, eventId, state, exn, formatter, Unchecked.defaultof<_>)
    member this.Enqueue (timestamp, categoryName, logLevel, eventId, state, exn, formatter, context) =
      this.Enqueue (timestamp, categoryName, logLevel, eventId, state, exn, formatter, context)
    member this.AsyncFlush () = this.AsyncFlush ()
    member this.Dispose () = this.Dispose ()