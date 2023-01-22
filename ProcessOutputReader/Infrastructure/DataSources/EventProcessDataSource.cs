using System.Diagnostics;
using ProcessOutputReader.Interfaces;
using ProcessOutputReader.Interfaces.Infrastructure;

namespace ProcessOutputReader.Infrastructure.DataSources
{
	internal class EventProcessDataSource : ProcessDataSourceBase, IDataSource
	{
		private readonly ProcessEventListener _listener;
		private readonly SemaphoreSlim _semaphore;
		private readonly CancellationTokenSource _cts;
		private readonly DisposableHelper _disposableHelper;

		private EventProcessDataSource(ProcessEventListener listener, IErrorFilter? errorFilter)
			: base(errorFilter)
		{
			_semaphore = new SemaphoreSlim(1);
			_listener = listener;
			_cts = new CancellationTokenSource();
			_disposableHelper = new DisposableHelper(GetType().Name);

			_listener.DataReceived += ListenerDataReceived;
			_listener.ErrorReceived += ListenerErrorReceived;
		}

		private async void ListenerDataReceived(string? value)
		{
			var token = _cts.Token;

			if (token.IsCancellationRequested)
				return;

			try
			{
				await _semaphore.WaitAsync(_cts.Token).ConfigureAwait(false);

				if (token.IsCancellationRequested)
					return;

				try
				{
					if (value == null)
					{
						StateMachine.SetDataReceiveStopped();

						await ReceiveHandlerAsync(value).ConfigureAwait(false);
					}
					else
					{
						using (StateMachine.SetDataReceived())
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
			catch (Exception ex)
			{
				Debugger.Break();
				var e = ex;
			}
		}

		private async void ListenerErrorReceived(string? error)
		{
			var token = _cts.Token;

			if (token.IsCancellationRequested)
				return;

			try
			{
				await _semaphore.WaitAsync(_cts.Token).ConfigureAwait(false);

				if (token.IsCancellationRequested)
					return;

				try
				{
					if (error == null)
					{
						StateMachine.SetErrorReceiveStopped();

						await ReceiveHandlerAsync(error).ConfigureAwait(false);
					}
					else
					{
						if (ErrorFilter == null || !ErrorFilter.Filter(error))
						{
							using (StateMachine.SetErrorReceived())
							{
								await ReceiveHandlerAsync(error).ConfigureAwait(false);
							}
						}
						else
						{
							using (StateMachine.SetDataReceived())
							{
								await ReceiveHandlerAsync(error).ConfigureAwait(false);
							}
						}
					}
				}
				finally
				{
					_semaphore.Release();
				}
			}
			catch (Exception ex)
			{
				Debugger.Break();
				var e = ex;
			}
		}

		public override Task StopUpdateAsync()
		{
			_disposableHelper.ThrowIfDisposed();

			_cts.Cancel();

			return _listener.StopAsync();
		}

		public void Dispose()
		{
			if (_disposableHelper.IsDisposed)
				return;

			_disposableHelper.SetIsDisposed();

			if (_listener != null!)
			{
				_listener.DataReceived -= ListenerDataReceived;
				_listener.ErrorReceived -= ListenerErrorReceived;

				_listener.Dispose();
			}

			if (_cts != null!)
			{
				_cts.Dispose();
			}

			if (_semaphore != null!)
			{
				_semaphore.Dispose();
			}
		}

		public static async Task<EventProcessDataSource> CreateAsync(string processName, string args, IErrorFilter? errorFilter)
		{
			var listener = await ProcessEventListener.CreateAsync(processName, args).ConfigureAwait(false);

			return new EventProcessDataSource(listener, errorFilter);
		}
	}
}