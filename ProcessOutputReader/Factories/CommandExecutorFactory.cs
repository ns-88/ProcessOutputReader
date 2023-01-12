using ProcessOutputReader.Interfaces;
using ProcessOutputReader.Interfaces.Factories;

namespace ProcessOutputReader.Factories
{
	public class CommandExecutorFactory : ICommandExecutorFactory
    {
	    public ICommandExecutor Create()
		{
			return new CommandExecutor(new ProcessDataSourceFactory());
		}
	}
}