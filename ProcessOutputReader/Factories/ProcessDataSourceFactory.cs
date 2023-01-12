using ProcessOutputReader.Infrastructure;
using ProcessOutputReader.Infrastructure.DataSources;
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

			var dataSource = await EventProcessDataSource.CreateAsync(command.ProcessPath, command.Args).ConfigureAwait(false);

			return dataSource;
		}
	}
}