using System.Buffers;
using System.Text;

namespace System;

public static class EncodingCompatExtensions
{
    public static int GetBytes(this Encoding encoding, ReadOnlySpan<char> chars, Span<byte> bytes)
    {
        var charBuffer = ArrayPool<char>.Shared.Rent(chars.Length);
        try
        {
            chars.CopyTo(charBuffer.AsSpan());
            var resSize = encoding.GetByteCount(charBuffer, 0, chars.Length);
            var byteBuffer = ArrayPool<byte>.Shared.Rent(resSize);
            try
            {
                encoding.GetBytes(charBuffer, 0, chars.Length, byteBuffer, 0);
                byteBuffer.AsSpan(0, resSize).CopyTo(bytes);
                return resSize;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(byteBuffer);
            }
        }
        finally
        {
            ArrayPool<char>.Shared.Return(charBuffer);
        }
    }

    public static int GetBytes(this Encoding encoding, string? chars, Span<byte> bytes)
        => encoding.GetBytes(chars.AsSpan(), bytes);
}