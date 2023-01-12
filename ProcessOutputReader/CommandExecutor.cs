using ProcessOutputReader.Infrastructure;
using ProcessOutputReader.Interfaces;
using ProcessOutputReader.Interfaces.Factories;

namespace ProcessOutputReader
{
	internal class CommandExecutor : ICommandExecutor
	{
		private readonly IDataSourceFactory _dataSourceFactory;

		public CommandExecutor(IDataSourceFactory dataSourceFactory)
		{
			Guard.ThrowIfNull(dataSourceFactory, out _dataSourceFactory);
		}

		public async Task ExecuteAsync(ICommand command)
		{
			Guard.ThrowIfNull(command);

			using var dataSource = await _dataSourceFactory.CreateAsync(command).ConfigureAwait(false);
			using var token = new WorkToken(dataSource);

			try
			{
				await command.ExecuteAsync(token).WaitAsync(command.Timeout).ConfigureAwait(false);
			}
			catch (TimeoutException)
			{
				try
				{
					await dataSource.StopUpdateAsync().ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					throw new InvalidOperationException(null, ex);
				}

				throw;
			}
		}
	}
}