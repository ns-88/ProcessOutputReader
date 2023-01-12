using ProcessOutputReader.Interfaces.Infrastructure;

namespace ProcessOutputReader.Interfaces.Factories
{
    internal interface IDataSourceFactory
    {
        Task<IDataSource> CreateAsync(ICommand command);
    }
}