module NCoreUtils.Logging.Unit.GoogleLoggerTests

open System
open System.Collections.Generic
open Google.Api
open Google.Api.Gax.Grpc
open Google.Cloud.Logging.V2
open Moq
open System.Threading
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open NCoreUtils.Logging
open Xunit

type Entry = {
  Labels    : Map<string, string>
  Timestamp : DateTimeOffset
  Message   : string
  Method    : string
  Url       : string
  UserAgent : string
  Referrer  : string
  RemoteIp  : string
  User      : string }

let createFakeGoogleClient (target : ICollection<_>) =
  let builder = Mock<LoggingServiceV2Client> ()

  let impl (_ : LogNameOneof) (_ : MonitoredResource) (labels : IDictionary<_, _>) (entries : seq<LogEntry>) (_ : CallSettings) =
    for entry in entries do
      match String.IsNullOrEmpty entry.TextPayload with
      | true ->
        let message = entry.JsonPayload.Fields.["message"].StringValue
        let context = entry.JsonPayload.Fields.["context"].StructValue
        let getValueSafe key =
          try context.Fields.[key].StringValue
          with _ -> null
        target.Add
          { Labels    = (if isNull labels then Map.empty else (labels |> Seq.map (fun (KeyValue (k, v)) -> (k, v)) |> Map.ofSeq))
            Timestamp = (DateTimeOffset.Parse entry.JsonPayload.Fields.["eventTime"].StringValue)
            Message   = message
            Method    = getValueSafe "method"
            Url       = getValueSafe "url"
            UserAgent = getValueSafe "userAgent"
            Referrer  = getValueSafe "referrer"
            RemoteIp  = getValueSafe "remoteIp"
            User      = getValueSafe "user" }
      | false ->
        target.Add
          { Labels    = (if isNull labels then Map.empty else (labels |> Seq.map (fun (KeyValue (k, v)) -> (k, v)) |> Map.ofSeq))
            Timestamp = entry.Timestamp.ToDateTimeOffset ()
            Message   = entry.TextPayload
            Method    = String.Empty
            Url       = String.Empty
            UserAgent = String.Empty
            Referrer  = String.Empty
            RemoteIp  = String.Empty
            User      = String.Empty }
    Task.FromResult (WriteLogEntriesResponse ())

  builder.Setup(fun client -> client.WriteLogEntriesAsync (It.IsAny<LogNameOneof> (), It.IsAny<MonitoredResource> (), It.IsAny<IDictionary<_, _>> (), It.IsAny<seq<LogEntry>> (), It.IsAny<CallSettings> ()))
    .Returns impl
    |> ignore

  builder.Object

[<Fact>]
let ``write google entries`` () =

  // if not(System.Diagnostics.Debugger.IsAttached) then
  //   printfn "Please attach a debugger, PID: %d" (System.Diagnostics.Process.GetCurrentProcess().Id)
  // while not(System.Diagnostics.Debugger.IsAttached) do
  //   System.Threading.Thread.Sleep(100)
  // System.Diagnostics.Debugger.Break()

  use fakeHttpContext =
    HttpContext.createFakeContext
      "GET"
      "http"
      (HostString "localhost")
      (PathString "/index.html")
      QueryString.Empty
      (Dictionary ())
      (ServiceCollection().AddPrePopulatedLoggingContext())
  let httpContextAccessor =
    { new IHttpContextAccessor with
        member __.HttpContext
          with get () = fakeHttpContext.HttpContext
          and  set _  = ()
    }
  let entries = ResizeArray ()
  let client = createFakeGoogleClient entries
  let conf : NCoreUtils.Logging.Google.GoogleAspNetCoreLoggingConfiguration =
    { ProjectId      = "proj"
      ServiceName    = "unit"
      ServiceVersion = null
      EnvPodName     = null
      EnvNodeName    = null }
  use sink = new NCoreUtils.Logging.Google.GoogleAspNetCoreSink (conf, fun () -> client)
  do
    use loggerProvider = new AspNetCoreLoggerProvider<NCoreUtils.Logging.Google.GoogleAspNetCoreSink> (sink, httpContextAccessor)
    let logger = loggerProvider.CreateLogger "category"
    logger.LogDebug "message"
    try invalidOp "exception"
    with e -> logger.LogError (e, "error message")
    while entries.Count < 2 do
      Thread.Sleep 50
  Assert.Equal (2, entries.Count)