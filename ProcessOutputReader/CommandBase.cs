using System.Diagnostics;
using ProcessOutputReader.Infrastructure;
using ProcessOutputReader.Interfaces;
using ProcessOutputReader.Interfaces.Infrastructure;

namespace ProcessOutputReader
{
	[DebuggerDisplay("ProcessPath = {ProcessPath}, Args = {Args}, State = {State}")]
	public abstract class CommandBase : ICommand
	{
		public string ProcessPath { get; }
		public string Args { get; }
		public TimeSpan Timeout { get; }
		public CommandStates State { get; private set; }

#nullable disable
		private CommandBase()
		{
			State = CommandStates.NotStarted;
		}
#nullable restore

		protected CommandBase(string processPath) : this()
		{
			ProcessPath = Guard.ThrowIfEmptyStringRet(processPath);
			Args = string.Empty;
			Timeout = TimeSpan.FromSeconds(50);
		}

		protected CommandBase(string processPath, string args) : this(processPath)
		{
			Args = Guard.ThrowIfEmptyStringRet(args);
			Timeout = TimeSpan.FromSeconds(50);
		}

		protected CommandBase(string processPath, string args, TimeSpan timeout) : this()
		{
			ProcessPath = Guard.ThrowIfEmptyStringRet(processPath);
			Args = Guard.ThrowIfEmptyStringRet(args);
			Timeout = timeout;
		}

		protected abstract void OnDataReceive(string value);

		protected static void StopReceiving()
		{
			throw new OperationCanceledException();
		}

		async Task ICommand.ExecuteAsync(IWorkToken token)
		{
			if (State != CommandStates.NotStarted)
			{
				throw new InvalidOperationException();
			}

			var wrapper = new WorkTokenWrapper(token, this);

			try
			{
				await wrapper.Task.ConfigureAwait(false);

				State = CommandStates.Completed;
			}
			catch (Exception)
			{
				State = CommandStates.Faulted;
				throw;
			}
		}

		#region Nested types

		private class WorkTokenWrapper
		{
			private readonly IWorkToken _workToken;
			private readonly TaskCompletionSource _tcs;
			private readonly CommandBase _command;

			public Task Task => _tcs.Task;

			public WorkTokenWrapper(IWorkToken workToken, CommandBase command)
			{
				workToken.DataReceived += OnDataReceived;
				workToken.ErrorReceived += OnErrorReceived;
				workToken.Completed += OnCompleted;

				_workToken = workToken;
				_command = command;
				_tcs = new TaskCompletionSource();
			}

			private void OnCompleted()
			{
				_tcs.SetResult();
			}

			private void OnErrorReceived(Exception ex)
			{
				_tcs.SetException(ex);
			}

			private async void OnDataReceived(string value)
			{
				try
				{
					_command.OnDataReceive(value);
				}
				catch (OperationCanceledException)
				{
					try
					{
						await _workToken.CancelAsync();
					}
					catch (Exception ex)
					{
						_tcs.SetException(ex);
						return;
					}

					_tcs.SetResult();
				}
			}
		}

		#endregion
	}
}