using System.Text;
using ProcessOutputReader.Interfaces;
using ProcessOutputReader.Interfaces.Infrastructure;

namespace ProcessOutputReader.Infrastructure.DataSources
{
	internal abstract class ProcessDataSourceBase
	{
		private readonly StringBuilder _errorBuilder;

		protected readonly StateMachine StateMachine;
		protected readonly IErrorFilter? ErrorFilter;

		public event Action<ChangedEventArgs>? Changed;

		protected ProcessDataSourceBase(IErrorFilter? errorFilter)
		{
			_errorBuilder = new StringBuilder();

			StateMachine = new StateMachine();
			ErrorFilter = errorFilter;
		}

		protected void ReceiveHandler(string? value)
		{
			switch (StateMachine.State)
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

						StopUpdateInternal(error);
					}
					break;

				case States.Undefined:
					{
						StopUpdateInternal(Strings.InvalidStateOfStateMachine);
					}
					break;

				case States.Nothing:
					return;

				default:
					throw new ArgumentOutOfRangeException(nameof(StateMachine.State), StateMachine.State, string.Format(Strings.UnknownEnumValue, nameof(States)));
			}
		}

		private void StopUpdateInternal(string? error)
		{
			Exception? exception = null;

			if (error != null)
			{
				if (ErrorFilter?.FinalFilter(error) == true)
				{
					RaiseChanged(ChangedEventArgs.FromValue(error));
				}
				else
				{
					exception = new InvalidOperationException(error);
				}
			}

			RaiseChanged(exception != null
				? ChangedEventArgs.FromError(exception)
				: ChangedEventArgs.FromCompleted());
		}

		private void RaiseChanged(ChangedEventArgs args)
		{
			Volatile.Read(ref Changed)?.Invoke(args);
		}

		public abstract Task StopUpdateAsync();
	}
}