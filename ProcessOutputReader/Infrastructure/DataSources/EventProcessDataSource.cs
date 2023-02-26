using ProcessOutputReader.Interfaces;
using ProcessOutputReader.Interfaces.Infrastructure;

namespace ProcessOutputReader.Infrastructure.DataSources
{
	internal class EventProcessDataSource : ProcessDataSourceBase, IDataSource
	{
		private readonly ProcessEventListener _listener;
		private readonly CancellationTokenSource _cts;
		private readonly DisposableHelper _disposableHelper;
		private readonly object _receivedLock;

		private EventProcessDataSource(ProcessEventListener listener, IErrorFilter? errorFilter)
			: base(errorFilter)
		{
			_listener = listener;
			_cts = new CancellationTokenSource();
			_disposableHelper = new DisposableHelper(GetType().Name);
			_receivedLock = new object();

			_listener.DataReceived += ListenerDataReceived;
			_listener.ErrorReceived += ListenerErrorReceived;
		}

		private void ListenerDataReceived(string? value)
		{
			var token = _cts.Token;

			if (token.IsCancellationRequested)
				return;

			lock (_receivedLock)
			{
				if (token.IsCancellationRequested)
					return;

				if (value == null)
				{
					StateMachine.SetDataReceiveStopped();

					ReceiveHandler(value);
				}
				else
				{
					using (StateMachine.SetDataReceived())
					{
						ReceiveHandler(value);
					}
				}
			}
		}

		private void ListenerErrorReceived(string? error)
		{
			var token = _cts.Token;

			if (token.IsCancellationRequested)
				return;

			lock (_receivedLock)
			{
				if (token.IsCancellationRequested)
					return;

				if (error == null)
				{
					StateMachine.SetErrorReceiveStopped();

					ReceiveHandler(error);
				}
				else
				{
					if (ErrorFilter == null || !ErrorFilter.Filter(error))
					{
						using (StateMachine.SetErrorReceived())
						{
							ReceiveHandler(error);
						}
					}
					else
					{
						using (StateMachine.SetDataReceived())
						{
							ReceiveHandler(error);
						}
					}
				}
			}
		}

		private void UnsubscribeEvents()
		{
			_listener.DataReceived -= ListenerDataReceived;
			_listener.ErrorReceived -= ListenerErrorReceived;
		}

		public override Task StopUpdateAsync()
		{
			_disposableHelper.ThrowIfDisposed();

			_cts.Cancel();

			UnsubscribeEvents();

			return _listener.StopAsync();
		}

		public void Dispose()
		{
			if (_disposableHelper.IsDisposed)
				return;

			_disposableHelper.SetIsDisposed();

			if (_cts != null!)
			{
				_cts.Dispose();
			}

			if (_listener == null!)
				return;

			UnsubscribeEvents();

			_listener.Dispose();
		}

		public static async Task<EventProcessDataSource> CreateAsync(string processName, string args, IErrorFilter? errorFilter)
		{
			var listener = await ProcessEventListener.CreateAsync(processName, args).ConfigureAwait(false);

			return new EventProcessDataSource(listener, errorFilter);
		}
	}
}