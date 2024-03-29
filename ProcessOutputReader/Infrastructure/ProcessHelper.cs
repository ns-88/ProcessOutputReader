﻿using System.Diagnostics;

namespace ProcessOutputReader.Infrastructure
{
	internal class ProcessHelper : IDisposable
	{
		private readonly TaskCompletionSource _tcs;
		private readonly DisposableHelper _disposableHelper;
		public readonly Process Process;

		private ProcessHelper(Process process)
		{
			_tcs = new TaskCompletionSource();
			_disposableHelper = new DisposableHelper(GetType().Name);

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
						throw new InvalidOperationException(string.Format(Strings.ProcessNotStarted, processName));
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
			_disposableHelper.ThrowIfDisposed();

			Process.Exited -= ProcessExited;

			try
			{
				Process.Kill();
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException(string.Format(Strings.ProcessNotStarted, Process.ProcessName), ex);
			}

			await Task.WhenAny(Process.WaitForExitAsync(), _tcs.Task).ConfigureAwait(false);
		}

		public void Dispose()
		{
			if (_disposableHelper.IsDisposed)
				return;

			_disposableHelper.SetIsDisposed();

			if (Process == null!)
				return;

			Process.Exited -= ProcessExited;
			Process.Dispose();
		}

		public static async Task<ProcessHelper> CreateAsync(string processName, string args)
		{
			var process = await InternalStartAsync(processName, args).ConfigureAwait(false);

			return new ProcessHelper(process);
		}
	}
}