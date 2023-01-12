using ProcessOutputReader.Infrastructure;
using ProcessOutputReader.Interfaces;
using ProcessOutputReader.Interfaces.Factories;
using ProcessOutputReader.Interfaces.Infrastructure;

namespace ProcessOutputReader.Factories
{
	internal class ProcessDataSourceFactory : IDataSourceFactory
	{
		public async Task<IDataSource> CreateAsync(ICommand command)
		{
			Guard.ThrowIfNull(command);

			var dataSource = await ProcessDataSource.CreateAsync(command.ProcessPath, command.Args).ConfigureAwait(false);

			return dataSource;
		}
	}
}