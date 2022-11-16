using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Logging.ByteSequences
{
#if NETSTANDARD2_1 || NETSTANDARD2_0
    public class JsonSerializedByteSequence<T>
#else
    public class JsonSerializedByteSequence<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>
#endif
        : IByteSequence
    {
        private static readonly byte[] _eol = Encoding.ASCII.GetBytes(Environment.NewLine);

        public T Value { get; }

        public JsonTypeInfo<T> TypeInfo { get; }

        public bool AppendLineBreak { get; }

        public JsonSerializedByteSequence(T value, JsonTypeInfo<T> typeInfo, bool appendLineBreak = true)
        {
            Value = value;
            TypeInfo = typeInfo;
            AppendLineBreak = appendLineBreak;
        }

        public async ValueTask WriteToAsync(IByteSequenceOutput output, CancellationToken cancellationToken = default)
        {
            await JsonSerializer.SerializeAsync(output.GetStream(), Value, TypeInfo, cancellationToken);
            if (AppendLineBreak)
            {
                await output.GetStream().WriteAsync(_eol.AsMemory(), CancellationToken.None);
            }
        }

        #region disposable

        protected virtual void Dispose(bool disposing) { /* noop */ }

        protected virtual ValueTask DisposeAsyncCore() => default;

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore();
            Dispose(disposing: false);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}