using ProcessOutputReader.Interfaces.Infrastructure;

namespace ProcessOutputReader.Interfaces
{
	public enum CommandStates
	{
		NotStarted,
		Completed,
		Faulted
	}

	public interface ICommand
	{
		string ProcessPath { get; }
		string Args { get; }
		TimeSpan Timeout { get; }
		CommandStates State { get; }

		internal Task ExecuteAsync(IWorkToken token);
	}
}