namespace NCoreUtils.Logging

open Microsoft.Extensions.Logging
open System
open System.Runtime.CompilerServices
open System.Threading

type private Dummy = class end

type Logger (provider : LoggerProvider, category : string) =
  /// Scope stack.
  let stack = ref []
  /// Creates new scope.
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member __.BeginScope state =
    let mutable success = false
    let mutable index = Unchecked.defaultof<_>
    while not success do
      let s = !stack
      index   <- List.length s
      success <- obj.ReferenceEquals(s, Interlocked.CompareExchange (stack, box state :: s, s))
    new LoggerScope (stack, index) :> IDisposable
  /// Creates and pushes log message into the provider specified queue
  abstract Log<'state> : logLevel:LogLevel * eventId:EventId * state:'state * exn:exn * formatter:Func<'state, exn, string> -> unit
  default __.Log (logLevel, eventId, state : 'state, exn, formatter) =
    provider.PushMessage <| LogMessage<'state> (category, logLevel, eventId, exn, state, formatter)
  interface ILogger with
    member __.IsEnabled _ = true
    member this.BeginScope state = this.BeginScope state
    member this.Log (logLevel, eventId, state : 'state, exn, formatter) =
      this.Log (logLevel, eventId, state, exn, formatter)

and LoggerProvider (sink : ISink) =
  /// <summary>
  /// Gets at most <paramref name="n" /> messages. Returns when either no pending messages are present or limit is
  /// reached.
  /// </summary>
  /// <param name="n">Message limit.</param>
  /// <param name="inbox">Mailbox processor to use.</param>
  /// <param name="enqueue">Function that is invoked to store the recieved messages.</param>
  static let rec fetchMessages n (inbox : MailboxProcessor<_>) (enqueue : _ -> unit) : Async<unit> = async {
    match n with
    | 0 -> ()
    | _ ->
      let! msg0 = inbox.TryReceive (timeout = 0)
      match msg0 with
      | Some msg ->
        enqueue msg
        return! fetchMessages (n - 1) inbox enqueue
      | _ -> () }
  /// <summary>
  /// Whether current instance has been disposed as <c>int</c> value (<c>0</c> - alive, <c>1</c> - disposed).
  /// </summary>
  let mutable isDisposed = 0
  /// <summary>
  /// Whether agent has exited. Used to await agent when disposing the provider object.
  /// </summary>
  let mutable isFinished = new ManualResetEventSlim (false)
  /// Cancellation passed to the agent.
  let cancellation = new CancellationTokenSource ()
  /// Inner loop when sink is capable of bulk operations.
  let rec bulkLoop (bulkSink : IBulkSink) (inbox : MailboxProcessor<_>) = async {
    let! (msg : LogMessage) = inbox.Receive ()
    do! async {
      use queue = bulkSink.CreateQueue ()
      msg.Enqueue queue
      do! fetchMessages 39 inbox (fun msg -> msg.Enqueue queue)
      if 0 = isDisposed then
        do! queue.AsyncFlush () }
    do! bulkLoop bulkSink inbox }
  /// Inner loop when sink is uncapable of bulk operations.
  let rec loop (sink : ISink) (inbox : MailboxProcessor<_>) = async {
    let! (msg : LogMessage) = inbox.Receive ()
    if 0 = isDisposed then
      do! msg.AsyncLog sink
    do! loop sink inbox }
  /// Adds cabcellation watch to the computation.
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  let watchCancellation computation (arg : MailboxProcessor<LogMessage>) = async {
    use! __ = Async.OnCancel isFinished.Set
    do! computation arg
    isFinished.Set () }
  /// Background message processing thread.
  let agent =
    match sink with
    | :? IBulkSink as bulkSink -> MailboxProcessor.Start (watchCancellation (bulkLoop bulkSink), cancellation.Token)
    | _                        -> MailboxProcessor.Start (watchCancellation (loop sink),         cancellation.Token)
  /// Underlying message sink.
  member internal __.Sink with [<MethodImpl(MethodImplOptions.AggressiveInlining)>] get () = sink
  /// Passes log message to the background thread.
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member __.PushMessage message = agent.Post message
  /// Creates logger for the specified category name.
  abstract CreateLogger : category:string -> Logger
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  default this.CreateLogger category = Logger (this, category)
  /// Performes resource cleanup.
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  member __.Dispose () =
    if 0 = Interlocked.CompareExchange (&isDisposed, 1, 0) then
      cancellation.Cancel ()
      if not (isFinished.Wait 150) then
        eprintfn "Logger provider cancellation failed."
      sink.Dispose ()
      cancellation.Dispose ()
      isFinished.Dispose ()
  interface IDisposable with
    member this.Dispose () = this.Dispose ()
  interface ILoggerProvider with
    member this.CreateLogger category = this.CreateLogger category :> _