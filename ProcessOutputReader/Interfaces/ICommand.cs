using ProcessOutputReader.Interfaces.Infrastructure;

namespace ProcessOutputReader.Interfaces
{
	public enum CommandStates
	{
		Started,
		NotStarted,
		Completed,
		Canceled,
		Faulted,
		Timeout
	}

	public interface ICommand
	{
		string ProcessPath { get; }
		string Args { get; }
		TimeSpan Timeout { get; }
		CommandStates State { get; }
		IErrorFilter? ErrorFilter { get; }

		internal void SetTimeoutState();

		internal Task ExecuteAsync(IWorkToken workToken, CancellationToken token = default);
	}
}