using System.Net;
using System.Net.Sockets;

namespace NCoreUtils.Logging.DefaultOutputs;

public class TcpOutput(IPEndPoint endPoint) : StreamOutput
{
    public IPEndPoint EndPoint { get; } = endPoint ?? throw new ArgumentNullException(nameof(endPoint));

    public TcpClient? Client { get; private set; }

    protected override Stream InitializeStream()
    {
        Client = new TcpClient();
        Client.Connect(EndPoint.Address, EndPoint.Port);
        return Client.GetStream();
    }

    #region disposable

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            Client?.Dispose();
        }
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        await base.DisposeAsyncCore().ConfigureAwait(false);
        Client?.Dispose();
    }

    #endregion
}