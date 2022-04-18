using System.Diagnostics.CodeAnalysis;

namespace NCoreUtils.Logging
{
    public interface IByteSequenceOutputFactory
    {
        bool TryCreate(string uriOrName, [MaybeNullWhen(false)] out IByteSequenceOutput output);
    }
}