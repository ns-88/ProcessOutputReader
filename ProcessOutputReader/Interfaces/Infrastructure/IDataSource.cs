namespace ProcessOutputReader.Interfaces.Infrastructure
{
    internal enum ChangedType
    {
        DataReceived,
        ErrorReceived,
        Completed
    }

    internal interface IDataSource : IDisposable
	{
        event Action<ChangedEventArgs>? Changed;
        Task StopUpdateAsync();
    }

    internal readonly struct ChangedEventArgs
    {
        public readonly ChangedType Type;
        public readonly string? Value;
        public readonly Exception? Exception;

        private ChangedEventArgs(ChangedType type, string? value, Exception? exception)
        {
            Type = type;
            Value = value;
            Exception = exception;
        }

        public static ChangedEventArgs FromValue(string value)
        {
            return new ChangedEventArgs(ChangedType.DataReceived, value, null);
        }

        public static ChangedEventArgs FromError(Exception exception)
        {
            return new ChangedEventArgs(ChangedType.ErrorReceived, null, exception);
        }

        public static ChangedEventArgs FromCompleted()
        {
            return new ChangedEventArgs(ChangedType.Completed, null, null);
        }
    }
}