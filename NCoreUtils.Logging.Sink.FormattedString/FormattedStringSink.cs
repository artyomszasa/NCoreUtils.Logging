using System.Buffers;

namespace NCoreUtils.Logging.FormattedString
{
    public class FormattedStringSink : GenericSink<(IMemoryOwner<byte> Owner, int Size)>
    {
        public FormattedStringSink(
            FormattedStringPayloadWriter payloadWriter,
            FormattedStringPayloadFactory payloadFactory)
            : base(payloadWriter, payloadFactory)
        { }
    }
}