using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NCoreUtils.Logging.DefaultOutputs
{
    public class TcpOutput : StreamOutput
    {
        public IPEndPoint EndPoint { get; }

        public TcpClient? Client { get; private set; }

        public TcpOutput(IPEndPoint endPoint)
            => EndPoint = endPoint ?? throw new System.ArgumentNullException(nameof(endPoint));

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
}