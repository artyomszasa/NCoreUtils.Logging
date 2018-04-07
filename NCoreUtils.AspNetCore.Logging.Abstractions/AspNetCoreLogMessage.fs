namespace NCoreUtils.Logging

open System
open Microsoft.Extensions.Logging
open NCoreUtils.Logging

type AspNetCoreLogMessage<'a> (category : string, logLevel : LogLevel, eventId : EventId, exn : exn, state : 'a, formatter : Func<'a, exn, string>, context : AspNetCoreContext) =
  inherit LogMessage<'a>(category, logLevel, eventId, exn, state, formatter)
  member val Context = context
  override this.AsyncLog sink =
    match sink with
    | :? IAspNetCoreSink as aspSink -> aspSink.AsyncLog (this.Timestamp, this.Category, this.LogLevel, this.EventId, this.State, this.Exception, this.Formatter, this.Context)
    | _                             -> base.AsyncLog sink
  override this.Enqueue queue =
    match queue with
    | :? IAspNetCoreSinkQueue as aspQueue -> aspQueue.Enqueue (this.Timestamp, this.Category, this.LogLevel, this.EventId, this.State, this.Exception, this.Formatter, this.Context)
    | _                                   -> base.Enqueue queue
