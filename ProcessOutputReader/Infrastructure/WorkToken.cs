﻿using ProcessOutputReader.Interfaces.Infrastructure;

namespace ProcessOutputReader.Infrastructure
{
    internal class WorkToken : IWorkToken
    {
        private readonly IDataSource _dataSource;

        public event Action<string>? DataReceived;
        public event Action<Exception>? ErrorReceived;
        public event Action? Completed;

        public WorkToken(IDataSource dataSource)
        {
            Guard.ThrowIfNull(dataSource, out _dataSource);

            _dataSource.Changed += DataSourceChanged;
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
            }
        }

        public Task CancelAsync()
        {
            return _dataSource.StopUpdateAsync();
        }
    }
}