using System.Diagnostics.CodeAnalysis;
using ProcessOutputReader.Infrastructure;
using ProcessOutputReader.Interfaces;
using ProcessOutputReader.Interfaces.Factories;
using ProcessOutputReader.Interfaces.Infrastructure;

namespace ProcessOutputReader
{
	[SuppressMessage("ReSharper", "MethodSupportsCancellation")]
	[SuppressMessage("Reliability", "CA2016:Forward the 'CancellationToken' parameter to methods", Justification = "<Ожидание>")]
	internal class CommandExecutor : ICommandExecutor
	{
		private readonly IDataSourceFactory _dataSourceFactory;

		public CommandExecutor(IDataSourceFactory dataSourceFactory)
		{
			Guard.ThrowIfNull(dataSourceFactory, out _dataSourceFactory);
		}

		public async Task ExecuteAsync(ICommand command, CancellationToken token)
		{
			Guard.ThrowIfNull(command);

			if (token.IsCancellationRequested)
			{
				throw new OperationCanceledException(Strings.CommandExecutionNotCompletedDueToCancellation);
			}

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

			using (var workToken = new WorkToken(dataSource))
			{
				try
				{
					await command.ExecuteAsync(workToken, token).WaitAsync(command.Timeout).ConfigureAwait(false);
				}
				catch (OperationCanceledException)
				{
					exceptions.Add(new OperationCanceledException(Strings.CommandExecutionNotCompletedDueToCancellation));
				}
				catch (TimeoutException ex)
				{
					command.SetTimeoutState();

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