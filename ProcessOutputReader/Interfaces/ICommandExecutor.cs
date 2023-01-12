namespace ProcessOutputReader.Interfaces
{
	public interface ICommandExecutor
	{
		Task ExecuteAsync(ICommand command);
	}
}