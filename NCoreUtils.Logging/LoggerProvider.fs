namespace NCoreUtils.Logging

open Microsoft.Extensions.Logging
open System
open System.Runtime.CompilerServices
open System.Threading
open System.Collections.Generic

type private Dummy = class end

[<AutoOpen>]
module private BulkLoopHelpers =
  let printErr =
    fun (exn : exn) ->
      eprintf "Failed to dispatch one or more log messages: %A" exn
      async.Zero ()

type private BulkLoop =
  val private sink : IBulkSink
  val private input : MailboxProcessor<LogMessage>
  val private isDisposed : unit -> bool
  val private dispatchMessages : IReadOnlyList<LogMessage> -> Async<unit>
  new (sink : IBulkSink, input, isDisposed) =
    let dispatchMessages (messages : IReadOnlyList<LogMessage>) =
      let queue = sink.CreateQueue ()
      messages |> Seq.iter (fun msg -> msg.Enqueue queue)
      match isDisposed () with
      | true -> async.Zero ()
      | _    -> async.TryWith (queue.AsyncFlush (), printErr)
    { sink             = sink
      input            = input
      isDisposed       = isDisposed
      dispatchMessages = dispatchMessages }
  member this.Execute (max : int, messages : List<_>) =
    match max - messages.Count with
    | 0 -> async.Return (messages :> IReadOnlyList<_>)
    | _ ->
      let inline tryReceive (_ : int) = this.input.TryReceive (timeout = 0)
      let inline handleOne (maybeMessage : LogMessage option) =
        match maybeMessage with
        | Some message ->
          messages.Add message
          this.Execute (max, messages)
        | _ -> this.Execute (messages.Count, messages)
      async.Bind (tryReceive Unchecked.defaultof<_>, handleOne)
  member this.Run (_ : int) =
    let inline receive (_ : int) =
      this.input.Receive ()
    let inline execute (message : LogMessage) =
      let messages = ResizeArray<_> 40
      messages.Add message
      async.Bind (this.Execute (40, messages), this.dispatchMessages)
    async.Bind (receive Unchecked.defaultof<_>, execute)
  member this.Loop (_ : int) =
    let inline loop () = this.Loop Unchecked.defaultof<_>
    async.Bind (this.Run Unchecked.defaultof<_>, loop)


type Logger (provider : LoggerProvider,  category : string) =
  /// Scopes
  let stack = ref []
  /// Creates new scope.
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  [<RequiresExplicitTypeArguments>]
  member __.BeginScope<'state> (state : 'state) =
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
    member this.BeginScope<'state> (state : 'state) = this.BeginScope<'state> state
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
      match! inbox.TryReceive (timeout = 0) with
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
  let rec bulkLoop (bulkSink : IBulkSink) (inbox : MailboxProcessor<_>) = // async {
    let loop = new BulkLoop (bulkSink, inbox, fun () -> 0 <> isDisposed)
    loop.Loop Unchecked.defaultof<_>
  /// Inner loop when sink is uncapable of bulk operations.
  let rec loop (sink : ISink) (inbox : MailboxProcessor<_>) = async {
    let! (msg : LogMessage) = inbox.Receive ()
    try
      if 0 = isDisposed then
        do! msg.AsyncLog sink
    with exn -> eprintfn "Failed to dispatch log message: %A" exn
    do! loop sink inbox }
  /// Adds cabcellation watch to the computation.
  [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
  let watchCancellation computation (arg : MailboxProcessor<LogMessage>) = async {
    try
      use! __ = Async.OnCancel isFinished.Set
      do! computation arg
    finally isFinished.Set () }
  /// Background message processing thread.
  let agent =
    match sink with
    | :? IBulkSink as bulkSink -> MailboxProcessor.Start (watchCancellation (bulkLoop bulkSink), cancellation.Token)
    | _                        -> MailboxProcessor.Start (watchCancellation (loop sink),         cancellation.Token)
  let agentErrorSubscription = agent.Error.Subscribe (fun e -> eprintfn "[%s] %A" (DateTimeOffset.Now.ToString("u")) e)
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
      if not (isFinished.Wait 200) then
        eprintfn "Logger provider cancellation failed."
      sink.Dispose ()
      agentErrorSubscription.Dispose ()
      cancellation.Dispose ()
      isFinished.Dispose ()
  interface IDisposable with
    member this.Dispose () = this.Dispose ()
  interface ILoggerProvider with
    member this.CreateLogger category = this.CreateLogger category :> _