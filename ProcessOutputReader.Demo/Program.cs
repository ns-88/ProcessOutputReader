using ProcessOutputReader.Factories;

namespace ProcessOutputReader.Demo
{
	internal class Program
	{
		static async Task Main(string[] args)
		{
			var factory = new CommandExecutorFactory();
			var executor = factory.Create();
			var command = new GetInterfacesInfoCommand();

			try
			{
				await executor.ExecuteAsync(command).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				var e = ex;
			}

			Console.ReadKey();
		}
	}

	internal class GetInterfacesInfoCommand : CommandBase
	{
		public readonly List<string> InterfacesInfo;

		public GetInterfacesInfoCommand()
			: base("ipconfig")
		{
			InterfacesInfo = new List<string>();
		}

		protected override void OnDataReceive(string value)
		{
			InterfacesInfo.Add(value);
		}
	}
}