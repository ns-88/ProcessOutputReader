using System.Diagnostics;

namespace ProcessOutputReader.Infrastructure
{
	internal class ProcessHelper : IDisposable
	{
		private readonly TaskCompletionSource _tcs;
		public readonly Process Process;

		private ProcessHelper(Process process)
		{
			_tcs = new TaskCompletionSource();

			Process = process;
			Process.Exited += ProcessExited;
		}

		private static Task<Process> InternalStartAsync(string processName, string args)
		{
			Guard.ThrowIfNull(processName);

			return Task.Run(() =>
			{
				var process = new Process
				{
					StartInfo = new ProcessStartInfo(processName, args)
					{
						UseShellExecute = false,
						CreateNoWindow = true,
						RedirectStandardOutput = true,
						RedirectStandardError = true
					},
					EnableRaisingEvents = true
				};

				try
				{
					if (!process.Start())
						throw new InvalidOperationException($"Процесс \"{processName}\" не был успешно запущен.");
				}
				catch (Exception)
				{
					process.Dispose();
					throw;
				}

				return process;
			});
		}

		private void ProcessExited(object? sender, EventArgs e)
		{
			if (Process.HasExited)
			{
				_tcs.SetResult();
			}
		}

		public async Task StopAsync()
		{
			Process.Exited -= ProcessExited;
			Process.Kill();

			try
			{
				await Task.WhenAny(Process.WaitForExitAsync(), _tcs.Task).ConfigureAwait(false);
			}
			finally
			{
				Process.Dispose();
			}
		}

		public static async Task<ProcessHelper> CreateAsync(string processName, string args)
		{
			var process = await InternalStartAsync(processName, args).ConfigureAwait(false);

			return new ProcessHelper(process);
		}

		public void Dispose()
		{
			if (Process == null!)
			{
				return;
			}

			Process.Exited -= ProcessExited;

			try
			{
				Process.Kill();
			}
			finally
			{
				Process.Dispose();
			}
		}
	}
}