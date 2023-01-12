using System.Text;
using ProcessOutputReader.Interfaces.Infrastructure;

namespace ProcessOutputReader.Infrastructure
{
    internal class ProcessDataSource : IDataSource
    {
	    private readonly ProcessEventListener _listener;
        private readonly StringBuilder _errorBuilder;
        private readonly StateMachine _stateMachine;
        private readonly SemaphoreSlim _semaphore;

        public event Action<ChangedEventArgs>? Changed;

        private ProcessDataSource(ProcessEventListener listener)
        {
	        _errorBuilder = new StringBuilder();
	        _listener = listener;
            _stateMachine = new StateMachine();
            _semaphore = new SemaphoreSlim(1);

            _listener.DataReceived += ListenerDataReceived;
            _listener.ErrorReceived += ListenerErrorReceived;
		}

        private async void ListenerDataReceived(string? value)
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            
            try
            {
                if (value == null)
                {
                    _stateMachine.SetDataReceiveStopped();

                    await ReceiveHandlerAsync(value).ConfigureAwait(false);
                }
                else
                {
                    using (_stateMachine.SetDataReceived())
                    {
                        await ReceiveHandlerAsync(value).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async void ListenerErrorReceived(string? error)
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);

			try
            {
                if (error == null)
                {
                    _stateMachine.SetErrorReceiveStopped();

                    await ReceiveHandlerAsync(error).ConfigureAwait(false);
                }
                else
                {
                    using (_stateMachine.SetErrorReceived())
                    {
                        await ReceiveHandlerAsync(error).ConfigureAwait(false);
                    }
                }
            }
            finally
			{
				_semaphore.Release();
			}
        }

        private async Task ReceiveHandlerAsync(string? value)
        {
            switch (_stateMachine.State)
            {
                case States.DataReceived:
                    {
                        RaiseChanged(ChangedEventArgs.FromValue(value!));
                    }
                    break;

                case States.ErrorReceived:
                    {
                        _errorBuilder.Append(value);
                    }
                    break;

                case States.ReceivingStopped:
                    {
                        string? error = null;

                        if (_errorBuilder.Length != 0)
                        {
                            error = _errorBuilder.ToString();
                        }

                        await StopUpdateInternalAsync(error).ConfigureAwait(false);
                    }
                    break;

                case States.Undefined:
                    {
                        const string error = "Недопустимое состояние конечного автомата.";

                        await StopUpdateInternalAsync(error).ConfigureAwait(false);
                    }
                    break;

                case States.Nothing:
                    return;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void RaiseChanged(ChangedEventArgs args)
        {
            Volatile.Read(ref Changed)?.Invoke(args);
        }

        private async Task StopUpdateInternalAsync(string? error)
        {
            Exception? exception = null;

            try
            {
                await StopUpdateAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            var exceptions = new List<Exception>();

            if (exception != null)
            {
                exceptions.Add(exception);
            }

            if (error != null)
            {
                exceptions.Add(new InvalidOperationException(error));
            }

            RaiseChanged(exceptions.Count != 0
                ? ChangedEventArgs.FromError(new AggregateException(exceptions))
                : ChangedEventArgs.FromCompleted());
        }

        public Task StopUpdateAsync()
        {
            return _listener.StopAsync();
        }

        public void Dispose()
        {
	        _listener.DataReceived -= ListenerDataReceived;
	        _listener.ErrorReceived -= ListenerErrorReceived;
            _listener.Dispose();
        }

		public static async Task<ProcessDataSource> CreateAsync(string processName, string args)
        {
	        var listener = await ProcessEventListener.CreateAsync(processName, args).ConfigureAwait(false);

	        return new ProcessDataSource(listener);
        }
    }
}