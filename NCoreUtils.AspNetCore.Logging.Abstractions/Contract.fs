namespace NCoreUtils.Logging

open System
open System.Collections.Generic
open System.Diagnostics.CodeAnalysis
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Primitives
open NCoreUtils.Logging

type private EmptyEnumerator<'T> () =
  interface System.Collections.IEnumerator with
    member __.Current = null
    member __.MoveNext () = false
    [<ExcludeFromCodeCoverage>]
    member __.Reset () = ()
  interface IEnumerator<'T> with
    member __.Current = Unchecked.defaultof<'T>
    member __.Dispose () = ()

/// Provides null safe implementation of the readonly dictionary.
[<Struct>]
[<NoEquality; NoComparison>]
type ReadOnlyDictionaryWrapper<'TKey, 'TValue> =
  internal
    { Instance : IReadOnlyDictionary<'TKey, 'TValue> }
  with
    member this.Count
      with [<MethodImpl(MethodImplOptions.AggressiveInlining)>] get () =
        match this.Instance with
        | null     -> 0
        | instance -> instance.Count
    member this.Item
      with [<MethodImpl(MethodImplOptions.AggressiveInlining)>] get key =
        match this.Instance with
        | null     -> KeyNotFoundException "Specified key could not be found." |> raise
        | instance -> instance.[key]
    member this.Keys
      with [<MethodImpl(MethodImplOptions.AggressiveInlining)>] get () =
        match this.Instance with
        | null     -> Seq.empty
        | instance -> instance.Keys
    member this.Values
      with [<MethodImpl(MethodImplOptions.AggressiveInlining)>] get () =
        match this.Instance with
        | null     -> Seq.empty
        | instance -> instance.Values
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member this.ContainsKey key =
      match this.Instance with
      | null     -> false
      | instance -> instance.ContainsKey key
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member this.TryGetValue (key, [<Out>] value: byref<_>) =
      match this.Instance with
      | null     ->
        value <- Unchecked.defaultof<_>
        false
      | instance -> instance.TryGetValue (key, &value)
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    member this.GetEnumerator () =
      match this.Instance with
      | null     -> new EmptyEnumerator<KeyValuePair<'TKey, 'TValue>> () :> IEnumerator<_>
      | instance -> instance.GetEnumerator ()
    interface System.Collections.IEnumerable with
      member this.GetEnumerator () = this.GetEnumerator () :> _
    interface IEnumerable<KeyValuePair<'TKey, 'TValue>> with
      member this.GetEnumerator () = this.GetEnumerator ()
    interface IReadOnlyCollection<KeyValuePair<'TKey, 'TValue>> with
      member this.Count = this.Count
    interface IReadOnlyDictionary<'TKey, 'TValue> with
      member this.Item with get key = this.[key]
      member this.Keys = this.Keys
      member this.Values = this.Values
      member this.ContainsKey key = this.ContainsKey key
      member this.TryGetValue (key, [<Out>] value : byref<_>) = this.TryGetValue (key, &value)

/// Represents actual ASP.NET Core context.
[<Struct>]
[<NoEquality; NoComparison>]
type AspNetCoreContext = {
  /// Connection id
  ConnectionId       : string
  /// Request method.
  Method             : string
  /// Requested URL.
  Url                : string
  /// User agent string.
  UserAgent          : string
  /// Referrer.
  Referrer           : string
  /// Response status code.
  ResponseStatusCode : Nullable<int>
  /// Remote ip address.
  RemoteIp           : string
  /// Request headers.
  Headers            : ReadOnlyDictionaryWrapper<string, StringValues>
  /// User if present.
  User               : string }

[<RequireQualifiedAccess>]
module AspNetCoreContext =

  [<CompiledName("Empty")>]
  let empty =
    { ConnectionId       = String.Empty
      Method             = String.Empty
      Url                = String.Empty
      UserAgent          = String.Empty
      Referrer           = String.Empty
      ResponseStatusCode = Nullable ()
      RemoteIp           = String.Empty
      Headers            = { Instance = null }
      User               = String.Empty }

/// Defines functionality to log messages with ASP.NET Core context.
type IAspNetCoreSink =
  inherit ISink
  abstract AsyncLog<'state>
    :  timestamp:DateTimeOffset
    *  categoryName:string
    *  logLevel:LogLevel
    *  eventId:EventId
    *  state:'state
    *  exn:exn
    *  formatter:Func<'state, exn, string>
    *  context:AspNetCoreContext
    -> Async<unit>

/// Defines functionality to queue messages with ASP.NET Core context.
type IAspNetCoreSinkQueue =
  inherit ISinkQueue
  abstract Enqueue<'state>
    :  timestamp:DateTimeOffset
    *  categoryName:string
    *  logLevel:LogLevel
    *  eventId:EventId
    *  state:'state
    *  exn:exn
    *  formatter:Func<'state, exn, string>
    *  context:AspNetCoreContext
    -> unit
