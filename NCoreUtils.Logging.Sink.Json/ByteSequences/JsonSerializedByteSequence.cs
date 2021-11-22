using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Logging.ByteSequences
{
    public class JsonSerializedByteSequence<T> : IByteSequence
    {
        private static readonly byte[] _eol = Encoding.ASCII.GetBytes(Environment.NewLine);

        public T Value { get; }

        public JsonSerializerOptions? Options { get; }

        public JsonTypeInfo<T>? TypeInfo { get; }

        public bool AppendLineBreak { get; }

        public JsonSerializedByteSequence(T value, JsonSerializerOptions? options = default, bool appendLineBreak = true)
        {
            Value = value;
            Options = options;
            AppendLineBreak = appendLineBreak;
        }

        public JsonSerializedByteSequence(T value, JsonTypeInfo<T> typeInfo, bool appendLineBreak = true)
        {
            Value = value;
            TypeInfo = typeInfo;
            AppendLineBreak = appendLineBreak;
        }

        public void Dispose() { /* noop */ }

        public ValueTask DisposeAsync()
            => default;

        public async ValueTask WriteToAsync(IByteSequenceOutput output, CancellationToken cancellationToken = default)
        {
            if (TypeInfo is not null)
            {
                await JsonSerializer.SerializeAsync<T>(output.GetStream(), Value, TypeInfo, cancellationToken);
            }
            else
            {
                await JsonSerializer.SerializeAsync(output.GetStream(), Value, Options, cancellationToken);
            }
            if (AppendLineBreak)
            {
                await output.GetStream().WriteAsync(_eol.AsMemory(), CancellationToken.None);
            }
        }
    }
}