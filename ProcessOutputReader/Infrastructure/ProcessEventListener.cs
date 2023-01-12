using System.Diagnostics;

namespace ProcessOutputReader.Infrastructure
{
	internal class ProcessEventListener : IDisposable
	{
		private readonly ProcessHelper _helper;
		private readonly DisposableHelper _disposableHelper;

		public event Action<string?>? DataReceived;
		public event Action<string?>? ErrorReceived;

		private ProcessEventListener(ProcessHelper helper)
		{
			_helper = helper;
			_disposableHelper = new DisposableHelper(GetType().Name);

			var process = _helper.Process;

			process.OutputDataReceived += ProcessOutputDataReceived;
			process.ErrorDataReceived += ProcessErrorDataReceived;

			process.BeginOutputReadLine();
			process.BeginErrorReadLine();
		}

		private void ProcessOutputDataReceived(object sender, DataReceivedEventArgs e)
		{
			Volatile.Read(ref DataReceived)?.Invoke(e.Data);
		}

		private void ProcessErrorDataReceived(object sender, DataReceivedEventArgs e)
		{
			Volatile.Read(ref ErrorReceived)?.Invoke(e.Data);
		}

		private void UnsubscribeEvents()
		{
			var process = _helper.Process;

			process.OutputDataReceived -= ProcessOutputDataReceived;
			process.ErrorDataReceived -= ProcessErrorDataReceived;

			process.CancelOutputRead();
			process.CancelErrorRead();
		}

		public async Task StopAsync()
		{
			_disposableHelper.ThrowIfDisposed();

			UnsubscribeEvents();

			await _helper.StopAsync().ConfigureAwait(false);
		}

		public void Dispose()
		{
			if (_disposableHelper.IsDisposed)
				return;

			_disposableHelper.SetIsDisposed();
			_helper.Dispose();
		}

		public static async Task<ProcessEventListener> CreateAsync(string processName, string args)
		{
			var helper = await ProcessHelper.CreateAsync(processName, args).ConfigureAwait(false);
			var listener = new ProcessEventListener(helper);

			return listener;
		}
	}
}