namespace NCoreUtils.Logging.Unit

open NCoreUtils.Logging

type TestMessageReceiver (messages : ResizeArray<_>) =
  let mutable index = 0
  interface IMessageReceiver<LogMessage> with
    member __.Receive _ =
      match index >= messages.Count with
      | true -> invalidOp "no pending messages"
      | _    ->
        let result = messages.[index]
        index <- index + 1
        async.Return result
    member __.TryReceive _ =
      match index >= messages.Count with
      | true -> async.Return None
      | _    ->
        let result = messages.[index]
        index <- index + 1
        async.Return (Some result)

type InfiniteMessagesWithDelay (message : LogMessage) =
  let mutable hasNext = false
  interface IMessageReceiver<LogMessage> with
    member __.Receive _ = async {
      do! Async.Sleep (100)
      hasNext <- true
      return message }
    member __.TryReceive _ =
      match hasNext with
      | true ->
        hasNext <- false
        Some message
      | _ ->  None
      |> async.Return


