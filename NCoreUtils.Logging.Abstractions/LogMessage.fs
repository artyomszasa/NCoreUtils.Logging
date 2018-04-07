namespace NCoreUtils.Logging

open Microsoft.Extensions.Logging
open System

[<AbstractClass>]
type LogMessage (category : string, logLevel : LogLevel, eventId : EventId, exn : exn) =
  member val Timestamp = DateTimeOffset.UtcNow
  member val Category  = category
  member val LogLevel  = logLevel
  member val EventId   = eventId
  member val Exception = exn
  abstract AsyncLog : sink:ISink -> Async<unit>
  abstract Enqueue : queue:ISinkQueue -> unit

type LogMessage<'a> (category, logLevel, eventId, exn, state : 'a, formatter : Func<'a, exn, string>) =
  inherit LogMessage (category, logLevel, eventId, exn)
  member val State     = state
  member val Formatter = formatter
  override this.AsyncLog sink =
    sink.AsyncLog (this.Timestamp, this.Category, this.LogLevel, this.EventId, this.State, this.Exception, this.Formatter)
  override this.Enqueue queue =
    queue.Enqueue (this.Timestamp, this.Category, this.LogLevel, this.EventId, this.State, this.Exception, this.Formatter)
