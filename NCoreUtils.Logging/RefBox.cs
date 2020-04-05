namespace NCoreUtils.Logging
{
    public sealed class RefBox<T>
    {
        public T Value { get; set; }

        public RefBox(T value)
            => Value = value;
    }
}