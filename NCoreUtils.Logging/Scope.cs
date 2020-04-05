namespace NCoreUtils.Logging
{
    internal class Scope
    {
        public static int Count(Scope? scope)
            => scope is null ? 0 : 1 + Count(scope.Next);

        public static Scope? Truncate(Scope? scope, int length)
            => scope is null
                ? null
                : length == 0
                    ? null
                    : new Scope(scope.Value, Truncate(scope.Next, length));

        public static Scope Append(Scope? scope, object? value)
            => scope is null
                ? new Scope(value, default)
                : new Scope(scope.Value, Append(scope.Next, value));

        public object? Value { get; }

        public Scope? Next { get; }

        public Scope(object? value, Scope? next)
        {
            Value = value;
            Next = next;
        }
    }
}