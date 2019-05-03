namespace NCoreUtils.Logging.Unit

open System
open System.Collections.Generic
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Primitives
open System.Net

type FakeHttpContext = {
  HttpContext  : HttpContext
  HttpRequest  : HttpRequest
  HttpResponse : HttpResponse
  Scope        : IServiceScope }
  with
    interface IDisposable with
      member this.Dispose () = this.Scope.Dispose ()

type FakeConnectionInfo () =
  inherit ConnectionInfo ()
  override val Id = "xxx" with get, set
  override val RemoteIpAddress = IPAddress.Loopback with get, set
  override val RemotePort = 65532 with get, set
  override val LocalIpAddress = IPAddress.Loopback with get, set
  override val LocalPort = 80 with get, set
  override val ClientCertificate = null with get, set
  override __.GetClientCertificateAsync _ = Task.FromResult null

module HttpContext =

  let createFakeContext
    (method : string)
    (scheme : string)
    (host: HostString)
    (path : PathString)
    (query: QueryString)
    (headers: IReadOnlyDictionary<string, string>)
    (services : IServiceCollection) =

      let connectionInfo = FakeConnectionInfo ()

      let requestBuilder = Moq.Mock<HttpRequest> ()
      requestBuilder.SetupGet(fun req -> req.Method)
        .Returns(method)
        |> ignore
      requestBuilder.SetupGet(fun req -> req.Scheme)
        .Returns(scheme)
        |> ignore
      requestBuilder.SetupGet(fun req -> req.Host)
        .Returns(host)
        |> ignore
      requestBuilder.SetupGet(fun req -> req.Path)
        .Returns(path)
        |> ignore
      requestBuilder.SetupGet(fun req -> req.QueryString)
        .Returns(query)
        |> ignore
      let headerDictionary =
        let d = new HeaderDictionary ()
        for kv in headers do
          d.Add (kv.Key, StringValues kv.Value)
        d
      requestBuilder.SetupGet(fun req -> req.Headers)
        .Returns(headerDictionary)
        |> ignore
      let request = requestBuilder.Object

      let responseBuilder = Moq.Mock<HttpResponse> ()
      responseBuilder.SetupProperty((fun resp -> resp.StatusCode), 200) |> ignore
      let response = responseBuilder.Object

      let scope = services.BuildServiceProvider(true).CreateScope ()
      let contextBuilder = Moq.Mock<HttpContext> ()
      contextBuilder.SetupGet(fun ctx -> ctx.Request)
        .Returns(request)
        |> ignore
      contextBuilder.SetupGet(fun ctx -> ctx.Response)
        .Returns(response)
        |> ignore
      contextBuilder.SetupGet(fun ctx -> ctx.RequestServices)
        .Returns(scope.ServiceProvider)
        |> ignore
      contextBuilder.SetupGet(fun ctx -> ctx.Connection)
        .Returns(connectionInfo)
        |> ignore
      contextBuilder.SetupProperty((fun ctx -> ctx.User), null) |> ignore

      { HttpContext  = contextBuilder.Object
        HttpRequest  = request
        HttpResponse = response
        Scope        = scope }