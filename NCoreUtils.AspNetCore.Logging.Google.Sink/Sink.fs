namespace NCoreUtils.Logging.Google

#nowarn "64"

open System
open System.Diagnostics.CodeAnalysis
open System.Runtime.CompilerServices
open System.Runtime.ExceptionServices
open System.Threading
open Google.Cloud.Logging.Type
open Google.Cloud.Logging.V2
open Google.Protobuf.WellKnownTypes
open Microsoft.Extensions.Logging
open NCoreUtils
open NCoreUtils.Logging

[<AutoOpen>]
module JsonPayload =

  [<AbstractClass; Sealed>]
  type ToValue =
    static member inline ToValue (_ : ToValue, string : string) = Value.ForString (string |?? String.Empty)
    static member inline ToValue (_ : ToValue, num : int) = Value.ForNumber (float num)
    static member inline ToValue (_ : ToValue, s : Struct) = Value.ForStruct s

  [<ExcludeFromCodeCoverage>]
  let inline private toValue (value : ^a) : Value
    when ^x :> ToValue
    and  (^a or ^x) : (static member ToValue : ^x * ^a -> Value)
    = ((^a or ^x) : (static member ToValue : ^x * ^a -> Value) (Unchecked.defaultof<ToValue>, value))

  [<ExcludeFromCodeCoverage>]
  let inline private add key value (obj : Struct) =
    obj.Fields.Add (key, toValue value)
    obj

  [<ExcludeFromCodeCoverage>]
  let inline private addNotNull key value (obj : Struct) =
    if not (String.IsNullOrEmpty value) then
      obj.Fields.Add (key, toValue value)
    obj

  [<ExcludeFromCodeCoverage>]
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
  abstract EnvPodName     : string
  abstract EnvNodeName    : string
  abstract PopulateLabels : timestamp:DateTimeOffset * category:string * logLevel:LogLevel * eventId:EventId * context:AspNetCoreContext * addLabel:Action<string, string> -> unit

[<CLIMutable>]
[<NoEquality; NoComparison>]
type GoogleAspNetCoreLoggingConfiguration = {
  ProjectId      : string
  ServiceName    : string
  ServiceVersion : string
  EnvPodName     : string
  EnvNodeName    : string }
  with
    interface IGoogleAspNetCoreLoggingConfiguration with
      member this.ProjectId      = this.ProjectId
      member this.ServiceName    = this.ServiceName
      member this.ServiceVersion = this.ServiceVersion
      member this.EnvPodName     = this.EnvPodName
      member this.EnvNodeName    = this.EnvNodeName
      member __.PopulateLabels (_, _, _, _, _, _) = ()

// GOOGLE SINK ---------------------------------------------------------------------------------------------------------

type [<Sealed>] GoogleAspNetCoreSink internal (configuration : IGoogleAspNetCoreLoggingConfiguration, factory : unit -> LoggingServiceV2Client) =

  [<Literal>]
  static let DefaultEnvPodName  = "KUBERNETES_POD_NAME"

  [<Literal>]
  static let DefaultEnvNodeName = "KUBERNETES_NODE_NAME"

  static let severityMap =
    [|
      LogSeverity.Debug
      LogSeverity.Debug
      LogSeverity.Info
      LogSeverity.Warning
      LogSeverity.Error
      LogSeverity.Critical
    |]

  static let resource = Google.Api.MonitoredResource (Type = "global")

  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static let mapSeverity logLevel =
    let l = int logLevel
    match l >= 0 && l <= 5 with
    | true -> severityMap.[l]
    | _    -> LogSeverity.Default

  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  static let (|RcpException|_|) = tryGetExn<Grpc.Core.RpcException>


  let client = factory ()
  [<VolatileField>]
  let mutable logName = null

  new (configuration) = new GoogleAspNetCoreSink (configuration, fun () -> LoggingServiceV2Client.Create ())

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
  member internal this.CreateLogEntry (timestamp : DateTimeOffset, categoryName : string, logLevel, eventId : EventId, state : 'state, exn : exn, formatter : Func<_, exn, string>, context : AspNetCoreContext) =
    let message =
      let stringBuilder =
        System.Text.StringBuilder()
          .Append('[')
          .Append(eventId.Id)
          .Append("] [")
          .Append(categoryName)
          .Append(' ')
          .Append(formatter.Invoke (state, exn))
      if not (isNull exn) then
        stringBuilder.AppendLine().Append exn |> ignore
      stringBuilder.ToString ()
    let jsonPayload =
      match isNull exn || isNull (box context) with
      | true -> null
      | _    -> mkJsonPayload timestamp configuration.ServiceName configuration.ServiceVersion message context
    let entry =
      LogEntry (
        LogName     = this.LogName.ToString (),
        Severity    = mapSeverity logLevel,
        Timestamp   = Timestamp.FromDateTimeOffset timestamp)
    configuration.PopulateLabels (timestamp, categoryName, logLevel, eventId, context, fun key value -> entry.Labels.Add (key, value))
    // generate default labels
    match Environment.GetEnvironmentVariable (configuration.EnvPodName |?? DefaultEnvPodName) with
    | null     -> ()
    | podName  -> entry.Labels.Add("k8s-pod", podName)
    match Environment.GetEnvironmentVariable (configuration.EnvNodeName |?? DefaultEnvNodeName) with
    | null     -> ()
    | nodeName -> entry.Labels.Add("k8s-node", nodeName)
    match context.ConnectionId with
    | null     -> ()
    | connId   -> entry.Labels.Add("asp-net-core-connection-id", connId)
    // create payload
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

// GOOGLE SINK QUEUE ---------------------------------------------------------------------------------------------------

and [<Sealed>] GoogleSinkQueue (sink : GoogleAspNetCoreSink) =
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

    member this.AsyncFlush () =
      this.AsyncFlush ()

    member this.Dispose () =
      this.Dispose ()