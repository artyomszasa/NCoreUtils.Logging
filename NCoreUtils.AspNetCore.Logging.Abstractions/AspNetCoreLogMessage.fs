namespace NCoreUtils.Logging

open System
open Microsoft.Extensions.Logging
open NCoreUtils.Logging

/// <summary>
/// Log message with ASP.NET Core specific message context.
/// </summary>
type AspNetCoreLogMessage<'a>  =
  inherit LogMessage<'a>
  val mutable context : AspNetCoreContext
  /// <summary>
  /// Initializes new instance from the specified parameters.
  /// </summary>
  /// <param name="category">The category name for message.</param>
  /// <param name="logLevel">Entry will be written on this level.</param>
  /// <param name="eventId">Id of the event.</param>
  /// <param name="exn">The exception related to this entry.</param>
  /// <param name="state">The entry to be written. Can be also an object.</param>
  /// <param name="formatter">Function to create a string message of the <paramref name="state" /> and <paramref name="exn" />.</param>
  /// <param name="context">ASP.NET Core specific message context.</param>
  new (category : string, logLevel : LogLevel, eventId : EventId, exn : exn, state : 'a, formatter : Func<'a, exn, string>, context : AspNetCoreContext) =
    { inherit LogMessage<'a> (category, logLevel, eventId, exn, state, formatter)
      context = context }
  /// ASP.NET Core specific message context.
  member this.Context = this.context
  /// Passes message to sink passing ASP.NET Core specific message context to sink when possible.
  override this.AsyncLog sink =
    match sink with
    | :? IAspNetCoreSink as aspSink -> aspSink.AsyncLog (this.Timestamp, this.Category, this.LogLevel, this.EventId, this.State, this.Exception, this.Formatter, this.Context)
    | _                             -> base.AsyncLog sink
  /// Passes message to queue passing ASP.NET Core specific message context to sink when possible.
  override this.Enqueue queue =
    match queue with
    | :? IAspNetCoreSinkQueue as aspQueue -> aspQueue.Enqueue (this.Timestamp, this.Category, this.LogLevel, this.EventId, this.State, this.Exception, this.Formatter, this.Context)
    | _                                   -> base.Enqueue queue
