namespace NCoreUtils.Logging

open System
open System.Threading

[<Sealed>]
type internal LoggerScope (stack : (obj list) ref, index : int) =
  let mutable isDisposed = 0
  interface IDisposable with
    member __.Dispose () =
      if 0 = Interlocked.CompareExchange (&isDisposed, 1, 0) then
        let mutable success = false
        while not success do
          let s = !stack
          match s with
          | _ :: s' when List.length s' >= index -> Interlocked.CompareExchange(stack, s', s) |> ignore
          | _ -> success <- true
