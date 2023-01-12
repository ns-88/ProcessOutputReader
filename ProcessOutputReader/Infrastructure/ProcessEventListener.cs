using System.Diagnostics;

namespace ProcessOutputReader.Infrastructure
{
    internal class ProcessEventListener : IDisposable
    {
	    private readonly string _processName;
	    private readonly string _args;
	    private readonly InitOnce<ProcessHelper> _helper;
	    private readonly SemaphoreSlim _semaphore;

        public event Action<string?>? DataReceived;
        public event Action<string?>? ErrorReceived;

        private ProcessEventListener(string processName, string args)
        {
	        _processName = processName;
	        _args = args;
	        _helper = new InitOnce<ProcessHelper>();
	        _semaphore = new SemaphoreSlim(1);
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
	        var process = _helper.Value.Process;
			
	        process.OutputDataReceived -= ProcessOutputDataReceived;
            process.ErrorDataReceived -= ProcessErrorDataReceived;

            process.CancelOutputRead();
            process.CancelErrorRead();
        }

        private async Task StartAsync()
        {
	        await _semaphore.WaitAsync().ConfigureAwait(false);

	        try
	        {
		        var helper = await ProcessHelper.CreateAsync(_processName, _args).ConfigureAwait(false);

		        _helper.Set(helper);

		        var process = _helper.Value.Process;

		        process.OutputDataReceived += ProcessOutputDataReceived;
		        process.ErrorDataReceived += ProcessErrorDataReceived;

		        process.BeginOutputReadLine();
		        process.BeginErrorReadLine();
	        }
	        finally
	        {
		        _semaphore.Release();
	        }
        }

        public async Task StopAsync()
        {
	        await _semaphore.WaitAsync().ConfigureAwait(false);

	        try
	        {
		        UnsubscribeEvents();

		        await _helper.Value.StopAsync().ConfigureAwait(false);
	        }
	        finally
	        {
		        _semaphore.Release();
	        }
        }

        public void Dispose()
        {
	        _semaphore.Dispose();
			_helper.Value.Dispose();
        }

        public static async Task<ProcessEventListener> CreateAsync(string processName, string args)
        {
	        var listener = new ProcessEventListener(processName, args);

	        await listener.StartAsync().ConfigureAwait(false);

	        return listener;
        }
    }
}