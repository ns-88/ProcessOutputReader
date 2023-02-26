using ProcessOutputReader.Interfaces.Infrastructure;

namespace ProcessOutputReader.Infrastructure
{
    internal class WorkToken : IWorkToken
    {
        private readonly IDataSource _dataSource;
        private readonly DisposableHelper _disposableHelper;

        public event Action<string>? DataReceived;
        public event Action<Exception>? ErrorReceived;
        public event Action? Completed;

        public WorkToken(IDataSource dataSource)
        {
            Guard.ThrowIfNull(dataSource, out _dataSource);

            _dataSource.Changed += DataSourceChanged;
            _disposableHelper = new DisposableHelper(GetType().Name);
        }

        private void DataSourceChanged(ChangedEventArgs args)
        {
            switch (args.Type)
            {
                case ChangedType.Completed:
                    {
                        Volatile.Read(ref Completed)?.Invoke();
                    }
                    break;

                case ChangedType.DataReceived:
                    {
                        Volatile.Read(ref DataReceived)?.Invoke(args.Value!);
                    }
                    break;

                case ChangedType.ErrorReceived:
                    {
                        Volatile.Read(ref ErrorReceived)?.Invoke(args.Exception!);
                    }
                    break;
                default:
	                throw new ArgumentOutOfRangeException(nameof(args.Type), args.Type, string.Format(Strings.UnknownEnumValue, nameof(ChangedType)));
            }
        }

        public Task CancelAsync()
        {
            _disposableHelper.ThrowIfDisposed();

            return _dataSource.StopUpdateAsync();
        }

        public void Dispose()
        {
	        if (_disposableHelper.IsDisposed)
		        return;

            _disposableHelper.SetIsDisposed();
			_dataSource.Changed -= DataSourceChanged;
		}
    }
}