using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Logging.Google
{
    public interface IGoogleFluentdSinkConfiguration : IGoogleSinkConfiguration
    {
        JsonSerializerOptions JsonSerializerOptions { get; }

        ValueTask<Stream> CreateOutputStreamAsync(CancellationToken cancellationToken = default);
    }
}