module NCoreUtils.Logging.Unit.ReadOnlyDictionaryWrapperTests

open System.Collections
open System.Collections.Generic
open NCoreUtils.Logging
open Xunit

[<Fact>]
let ``empty`` () =
  let d = Unchecked.defaultof<ReadOnlyDictionaryWrapper<string, string>> :> IReadOnlyDictionary<_, _>
  Assert.Equal (0, d.Count)
  Assert.Empty (d.Keys)
  Assert.Empty (d.Values)
  Assert.Throws<KeyNotFoundException> (fun () -> ignore d.["x"]) |> ignore
  Assert.False (d.ContainsKey "x")
  do
    use e = d.GetEnumerator ()
    Assert.False (e.MoveNext ())
    Assert.Equal (Unchecked.defaultof<_>, e.Current)
    Assert.Null ((e :> IEnumerator).Current)
    Assert.False (e.MoveNext ())
  let mutable value = Unchecked.defaultof<_>
  Assert.False (d.TryGetValue ("x", &value))

[<Fact>]
let ``non-empty`` () =
  let d =
    let d = new Dictionary<string, string> ()
    d.Add ("x", "y")
    { Instance = d }
  Assert.Equal (1, d.Count)
  Assert.True (Array.forall2 (=) (Array.ofSeq d.Keys) [| "x" |])
  Assert.True (Array.forall2 (=) (Array.ofSeq d.Values) [| "y" |])
  Assert.Throws<KeyNotFoundException> (fun () -> ignore d.["y"]) |> ignore
  Assert.Equal ("y", d.["x"])
  Assert.True (d.ContainsKey "x")
  Assert.False (d.ContainsKey "y")
  do
    use e = d.GetEnumerator ()
    Assert.True (e.MoveNext ())
    Assert.Equal (KeyValuePair ("x", "y"), e.Current)
    Assert.False (e.MoveNext ())
  do
    let e = (d :> IEnumerable).GetEnumerator ()
    Assert.True (e.MoveNext ())
    Assert.Equal (KeyValuePair ("x", "y"), Assert.IsType<KeyValuePair<string, string>> e.Current)
    Assert.False (e.MoveNext ())
  let mutable value = Unchecked.defaultof<_>
  Assert.True (d.TryGetValue ("x", &value))
  Assert.Equal ("y", value)
  Assert.False (d.TryGetValue ("y", &value))
