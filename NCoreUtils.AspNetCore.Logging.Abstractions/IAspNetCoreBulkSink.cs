namespace NCoreUtils.Logging
{
    public interface IAspNetCoreBulkSink : IAspNetCoreSink, IBulkSink
    {
        new IAspNetCoreSinkQueue CreateQueue();
    }
}