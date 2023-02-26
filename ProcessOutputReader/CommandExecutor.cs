using ProcessOutputReader.Infrastructure;
using ProcessOutputReader.Interfaces;
using ProcessOutputReader.Interfaces.Factories;
using ProcessOutputReader.Interfaces.Infrastructure;

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

			IDataSource dataSource;
			var exceptions = new List<Exception>();

			try
			{
				dataSource = await _dataSourceFactory.CreateAsync(command).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException(Strings.StartReceivingDataNotCompleted, ex);
			}

			using (var token = new WorkToken(dataSource))
			{
				try
				{
					await command.ExecuteAsync(token).WaitAsync(command.Timeout).ConfigureAwait(false);
				}
				catch (OperationCanceledException)
				{
					exceptions.Add(new OperationCanceledException(Strings.CommandExecutionNotCompletedDueToCancellation));
				}
				catch (TimeoutException ex)
				{
					exceptions.Add(new TimeoutException(string.Format(Strings.CommandExecutionTimeout, command.Timeout), ex));
				}
				catch (Exception ex)
				{
					exceptions.Add(new InvalidOperationException(Strings.CommandExecutionNotCompleted, ex));
				}
			}

			try
			{
				await dataSource.StopUpdateAsync().ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				exceptions.Add(new InvalidOperationException(Strings.StopReceivingDataNotCompleted, ex));
			}
			finally
			{
				dataSource.Dispose();
			}

			if (exceptions.Count != 0)
			{
				throw new AggregateException(exceptions);
			}
		}
	}
}