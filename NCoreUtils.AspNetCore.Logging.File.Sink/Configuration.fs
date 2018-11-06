namespace NCoreUtils.Logging.File

open System
open System.IO
open System.Text
open Microsoft.Extensions.Logging
open NCoreUtils
open NCoreUtils.Logging
open System.IO.Compression
open System.Collections.Generic
open System.Threading
open System
open System.Text.RegularExpressions

type FileVersioningStrategy =
  | None        = 0
  | Incremental = 1
  | ByDate      = 2
  // | ByHour      = 3

type FileLogEntryFormatting =
  | Json
  | PlainText of FormatString:string

type IFileAspNetCoreLoggingConfiguration =
  abstract Path                : string
  abstract MaxFileSize         : Nullable<int64>
  abstract VersioningStrategy  : FileVersioningStrategy
  abstract MaxVersions         : int
  abstract AutoFlush           : bool
  abstract CompressOldVersions : bool
  abstract Encoding            : Encoding
  abstract LogEntryFormatting  : FileLogEntryFormatting
  abstract PopulateLabels      : timestamp:DateTimeOffset * category:string * logLevel:LogLevel * eventId:EventId * context:AspNetCoreContext * addLabel:Action<string, string> -> unit

[<AutoOpen>]
module private FileAspNetCoreSinkHelpers =

  let private memoise f =
    let cache = Dictionary ()
    fun arg ->
      let mutable result = Unchecked.defaultof<_>
      if not (cache.TryGetValue (arg, &result)) then
        result <- f arg
        cache.[arg] <- result
      result

  type ILogEntryFormatter =
    abstract FormatEntry : timestamp:DateTimeOffset * category:string * logLevel:LogLevel * eventId:EventId * context:AspNetCoreContext * labels:string[] -> byte[]

  type IFileWriter =
    inherit IDisposable
    abstract AsyncWriteEntry : timestamp:DateTimeOffset * category:string * logLevel:LogLevel * eventId:EventId * context:AspNetCoreContext * labels:string[] -> Async<unit>

  type IBulkFileWriter =
    inherit IDisposable
    abstract AsyncWriteEntry : timestamp:DateTimeOffset * category:string * logLevel:LogLevel * eventId:EventId * context:AspNetCoreContext * labels:string[] -> Async<unit>
    abstract AsyncFlush : unit -> Async<unit>

  type private RollOperationType =
    | Copy     = 0
    | Compress = 1
    | Remove   = 2

  [<Struct>]
  [<NoEquality; NoComparison>]
  type private RollOperation = {
    OperationType : RollOperationType
    SourcePath    : string
    TargetPath    : string }

  let private openWrite (path : string) = new FileStream (path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 8192, true)

  let private openWriteInitial (path : string) = new FileStream (path, FileMode.Append, FileAccess.Write, FileShare.None, 8192, true)

  [<AbstractClass>]
  type private RollingFileWriter (entryFormatter : ILogEntryFormatter, initialStream : Stream) =

    let mutable isDisposed = 0

    let mutable stream = initialStream

    member __.Stream with get () = stream and set value = stream <- value

    abstract Check : data:byte[] -> Async<bool>

    /// Populates roll operations in strict last to first order and the path of the file to pass further logging into.
    abstract PopulateRollOperations : unit -> struct (IReadOnlyList<RollOperation> * string)

    abstract Roll : unit -> Async<Stream>

    abstract CheckAndRoll : data:byte[] -> Async<unit>

    default this.Roll () = async {
      do! this.Stream.AsyncFlush ()
      this.Stream.Close ()
      this.Stream.Dispose ()
      let struct (toRotate, path) = this.PopulateRollOperations ()

      for { OperationType = op; SourcePath = sourcePath; TargetPath = targetPath } in toRotate do
        match op with
        | RollOperationType.Copy     -> File.Move (sourcePath, targetPath)
        | RollOperationType.Remove   -> File.Delete sourcePath
        | RollOperationType.Compress ->
          do! async {
            use oldStream = new FileStream (sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, FileOptions.Asynchronous ||| FileOptions.SequentialScan)
            use newStream = openWrite targetPath
            use compressStream = new GZipStream (newStream, CompressionLevel.Optimal)
            do! Stream.asyncCopyToWithBufferSize 8192 compressStream oldStream
            do! compressStream.AsyncFlush () }
          File.Delete sourcePath
        | _ -> invalidOpf "Invalid roll operation: %A" op
      return openWrite path :> Stream }

    default this.CheckAndRoll data = async {
      match! this.Check data with
      | true ->
        let! stream' = this.Roll ()
        this.Stream <- stream'
      | _ -> () }

    member this.AsyncWriteEntryInternal (timestamp, category, logLevel, eventId, context, labels) = async {
      let data = entryFormatter.FormatEntry (timestamp, category, logLevel, eventId, context, labels)
      do! this.CheckAndRoll data
      do! this.Stream.AsyncWrite data }

    interface IDisposable with
      member this.Dispose () =
        if 0 = Interlocked.CompareExchange (&isDisposed, 1, 0) then
          this.Stream.Dispose ()

    interface IFileWriter with
      member this.AsyncWriteEntry (timestamp, category, logLevel, eventId, context, labels) = async {
        do! this.AsyncWriteEntryInternal (timestamp, category, logLevel, eventId, context, labels)
        do! this.Stream.AsyncFlush () }

    interface IBulkFileWriter with
      member this.AsyncWriteEntry (timestamp, category, logLevel, eventId, context, labels) =
        this.AsyncWriteEntryInternal (timestamp, category, logLevel, eventId, context, labels)
      member this.AsyncFlush () = this.Stream.AsyncFlush ()

  // Incremental rolling file writer
  [<Sealed>]
  type private IncrementalRollingFileWriter (mkPath : int -> string, entryFormatter, maxFileSize, maxVersions : int, compressOldVersions) =
    inherit RollingFileWriter (entryFormatter, openWriteInitial <| mkPath 0)
    override this.Check data =
      let result =
        let streamLength = this.Stream.Length
        match streamLength with
        // if single entry is larger than max files size
        | 0L when data.LongLength > maxFileSize -> false
        | _                                     -> streamLength + data.LongLength > maxFileSize
      async.Return result


    override __.PopulateRollOperations () =
      let ops = ResizeArray maxVersions
      let mkPath = memoise mkPath
      let rec search i =
        match mkPath i with
        | path when File.Exists path ->
          ops.Add <|
            match maxVersions >= i with
            | true                         -> { OperationType = RollOperationType.Remove;   SourcePath = path; TargetPath = null }
            | _ ->
              match i with
              | 0 when compressOldVersions -> { OperationType = RollOperationType.Compress; SourcePath = path; TargetPath = mkPath (i + 1) }
              | _                          -> { OperationType = RollOperationType.Copy;     SourcePath = path; TargetPath = mkPath (i + 1) }
          search (i + 1)
        | _ -> ()
      search 0
      ops.Reverse ()
      struct (ops :> _, mkPath 0)

  [<Struct>]
  [<NoEquality; NoComparison>]
  type DateFormatInfo = {
    Folder    : string
    Format    : DateTime -> string
    Recognize : string -> bool }

  let private emptyOps = [||] :> IReadOnlyList<RollOperation>

  [<Sealed>]
  type private DailyRollingFileWriter  =
    inherit RollingFileWriter
    val private fmt                 : DateFormatInfo
    val private compressOldVersions : bool
    val private maxVersions         : int
    val mutable private ts          : DateTime
    new (fmt : DateFormatInfo, entryFormatter, maxVersions : int, compressOldVersions) =
      let ts = DateTime.Now
      { inherit RollingFileWriter (entryFormatter, openWriteInitial (fmt.Format ts))
        fmt                 = fmt
        maxVersions         = maxVersions
        compressOldVersions = compressOldVersions
        ts                  = ts }
    override this.Check _ =
      let now = DateTime.Now
      async.Return (now.Day <> this.ts.Day || now.Month <> this.ts.Month || now.Year <> this.ts.Year)
    override this.PopulateRollOperations () =
      let mkPath = memoise this.fmt.Format
      this.ts <- DateTime.Now
      let current = mkPath this.ts
      let ops =
        match this.compressOldVersions with
        | false -> emptyOps
        | _     ->
          Directory.GetFiles this.fmt.Folder
          |> Seq.filter (fun path -> this.fmt.Recognize path && not (path.EndsWith ".gz"))
          |> Seq.map    (fun path -> { OperationType = RollOperationType.Compress; SourcePath = path; TargetPath = path + ".gz" })
          |> Seq.toArray
          :> _
      struct (ops, current)

  let private regex = Regex ("\\{version\\}", RegexOptions.Compiled ||| RegexOptions.IgnoreCase ||| RegexOptions.CultureInvariant)

  let mkIntPathFormatter (pattern : string) =
    let parts = regex.Split pattern
    fun i -> String.concat (sprintf "%d" i) parts

  let mkDayPathFormatter (pattern : string) =
    let parts = regex.Split pattern
    fun (dt : DateTime) -> String.concat (dt.ToString "yyyyMMdd") parts

  type private OutputPart =
    | Constant of Value:string
    |

  type PlainTextEntryFormatter (pattern : string, encoding : Encoding) =
    // writeout is running on a separate thread so a single instance should be enough
    let buffer = new MemoryStream (4096)
    //timestamp:DateTimeOffset * category:string * logLevel:LogLevel * eventId:EventId * context:AspNetCoreContext * labels:string[] -> byte[]


type [<Sealed>] FileAspNetCoreSink =
  val private writer : IFileWriter
  new (configuration : IFileAspNetCoreLoggingConfiguration) =
    let writer =
      match configuration.VersioningStrategy with
      | FileVersioningStrategy.None -> notImplemented "WIP"
      | FileVersioningStrategy.Incremental ->
        let mkPath =
          let formatter = mkIntPathFormatter configuration.Path
          match configuration.CompressOldVersions with
          | false -> formatter
          | true -> function | 0 -> formatter 0 | i -> formatter i + ".gz"
    { }
