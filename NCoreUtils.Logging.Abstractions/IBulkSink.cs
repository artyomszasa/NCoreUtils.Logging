namespace NCoreUtils.Logging
{
    public interface IBulkSink : ISink
    {
        ISinkQueue CreateQueue();
    }
}