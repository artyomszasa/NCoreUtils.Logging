module NCoreUtils.Logging.Unit.LoggerTests

open System.Collections.Generic
open System.Security.Claims
open System.Threading
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open NCoreUtils.Logging
open Xunit

let private category = "category"
let private eventId = EventId (1000, "event")
let private logLevel = LogLevel.Debug

[<Fact>]
let ``cancellation with bulk sink`` () =
  let entries = ResizeArray ()
  let sink = new TestBulkSink (entries)
  do
    use loggerProvider = new LoggerProvider (sink)
    Assert.Same (sink, loggerProvider.Sink)
    let logger = (loggerProvider :> ILoggerProvider).CreateLogger category
    Assert.True (logger.IsEnabled logLevel)
    logger.Log (logLevel, eventId, "message")
    Assert.Empty ((logger :?> Logger).Scopes)
    let scopeDispose = logger.BeginScope "scope"
    Assert.Equal (1, (logger :?> Logger).Scopes.Length)
    logger.Log (logLevel, eventId, "message")
    scopeDispose.Dispose ()
    Assert.Empty ((logger :?> Logger).Scopes)
    Thread.Sleep 200
    // check dispose reentrance
    loggerProvider.Dispose ()
  Assert.Equal (2, entries.Count)

[<Fact>]
let ``cancellation with simple sink`` () =
  let entries = ResizeArray ()
  let sink = new TestSink (entries)
  do
    use loggerProvider = new LoggerProvider (sink)
    Assert.Same (sink, loggerProvider.Sink)
    let logger = (loggerProvider :> ILoggerProvider).CreateLogger category
    logger.Log (logLevel, eventId, "message")
    logger.Log (logLevel, eventId, "message")
    Thread.Sleep 200
    // check dispose reentrance
    loggerProvider.Dispose ()
  Assert.Equal (2, entries.Count)

[<Fact>]
let ``errors with bulk sink`` () =
  let sink = new ErrorBulkSink ()
  do
    use loggerProvider = new LoggerProvider (sink)
    let logger = (loggerProvider :> ILoggerProvider).CreateLogger category
    logger.Log (logLevel, eventId, "message")
    logger.Log (logLevel, eventId, "message")
    Thread.Sleep 200
    // check dispose reentrance
    loggerProvider.Dispose ()

[<Fact>]
let ``errors with simple sink`` () =
  let sink = new ErrorSink ()
  do
    use loggerProvider = new LoggerProvider (sink)
    let logger = (loggerProvider :> ILoggerProvider).CreateLogger category
    logger.Log (logLevel, eventId, "message")
    logger.Log (logLevel, eventId, "message")
    Thread.Sleep 200
    // check dispose reentrance
    loggerProvider.Dispose ()

let private ``aspnetcore sink``<'a, 'entry when 'a :> ISink> (sink: 'a) (entries : ResizeArray<'entry>) =
  do
    use fakeHttpContext =
      HttpContext.createFakeContext
        "GET"
        "http"
        (HostString "localhost")
        (PathString "/index.html")
        QueryString.Empty
        (Dictionary ())
        (ServiceCollection ())
    let httpContextAccessor =
      { new IHttpContextAccessor with
          member __.HttpContext
            with get () = fakeHttpContext.HttpContext
            and  set _  = ()
      }
    use loggerProvider = new AspNetCoreLoggerProvider<'a> (sink, httpContextAccessor)
    Assert.Same (sink, loggerProvider.Sink)
    let logger = (loggerProvider :> ILoggerProvider).CreateLogger category
    logger.Log (logLevel, eventId, "message")
    logger.Log (logLevel, eventId, "message")
    Thread.Sleep 200
    // check dispose reentrance
    loggerProvider.Dispose ()
  Assert.Equal (2, entries.Count)

let private ``aspnetcore sink with context``<'a, 'entry when 'a :> ISink> (sink: 'a) (entries : ResizeArray<'entry>) =
  do
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
    use loggerProvider = new AspNetCoreLoggerProvider<'a> (sink, httpContextAccessor)
    Assert.Same (sink, loggerProvider.Sink)
    let logger = (loggerProvider :> ILoggerProvider).CreateLogger category
    logger.Log (logLevel, eventId, "message")
    logger.Log (logLevel, eventId, "message")
    Thread.Sleep 200
    // check dispose reentrance
    loggerProvider.Dispose ()
  Assert.Equal (2, entries.Count)


[<Fact>]
let ``aspnetcore simple sink`` () =
  do
    let entries = ResizeArray ()
    let sink = new TestSink (entries)
    ``aspnetcore sink`` sink entries
  do
    let entries = ResizeArray ()
    let sink = new TestAspSink (entries)
    ``aspnetcore sink`` sink entries

[<Fact>]
let ``aspnetcore bulk sink`` () =
  do
    let entries = ResizeArray ()
    let sink = new TestBulkSink (entries)
    ``aspnetcore sink`` sink entries
  do
    let entries = ResizeArray ()
    let sink = new TestAspBulkSink (entries)
    ``aspnetcore sink`` sink entries

[<Fact>]
let ``aspnetcore simple sink with context`` () =
  do
    let entries = ResizeArray ()
    let sink = new TestSink (entries)
    ``aspnetcore sink with context`` sink entries
  do
    let entries = ResizeArray ()
    let sink = new TestAspSink (entries)
    ``aspnetcore sink with context`` sink entries

[<Fact>]
let ``aspnetcore bulk sink with context`` () =
  do
    let entries = ResizeArray ()
    let sink = new TestBulkSink (entries)
    ``aspnetcore sink with context`` sink entries
  do
    let entries = ResizeArray ()
    let sink = new TestAspBulkSink (entries)
    ``aspnetcore sink with context`` sink entries



[<Fact>]
let ``aspnetcore user change`` () =

  // if not(System.Diagnostics.Debugger.IsAttached) then
  //   printfn "Please attach a debugger, PID: %d" (System.Diagnostics.Process.GetCurrentProcess().Id)
  // while not(System.Diagnostics.Debugger.IsAttached) do
  //   System.Threading.Thread.Sleep(100)
  // System.Diagnostics.Debugger.Break()

  let entries = ResizeArray ()
  let sink = new TestAspBulkSink (entries)
  do
    use fakeHttpContext =
      HttpContext.createFakeContext
        "GET"
        "http"
        (HostString "localhost")
        (PathString "/index.html")
        QueryString.Empty
        (Dictionary ())
        (ServiceCollection ())
    let httpContextAccessor =
      { new IHttpContextAccessor with
          member __.HttpContext
            with get () = fakeHttpContext.HttpContext
            and  set _  = ()
      }
    use loggerProvider = new AspNetCoreLoggerProvider<TestAspBulkSink> (sink, httpContextAccessor)
    Assert.Same (sink, loggerProvider.Sink)
    let logger = (loggerProvider :> ILoggerProvider).CreateLogger category
    logger.Log (logLevel, eventId, "message")
    let user =
      let identity =
        ClaimsIdentity (
          [| Claim (ClaimTypes.Name, "user") |],
          "auth",
          ClaimTypes.Name,
          ClaimTypes.Role)
      ClaimsPrincipal (identity)
    fakeHttpContext.HttpContext.User <- user
    logger.Log (logLevel, eventId, "message")
    Thread.Sleep 200
    // check dispose reentrance
    loggerProvider.Dispose ()
  Assert.Equal (2, entries.Count)
  Assert.True (System.String.IsNullOrEmpty entries.[0].Context.User)
  Assert.Equal ("user", entries.[1].Context.User)

