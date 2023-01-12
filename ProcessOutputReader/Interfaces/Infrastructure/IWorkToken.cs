namespace ProcessOutputReader.Interfaces.Infrastructure
{
    internal interface IWorkToken : IDisposable
    {
        event Action<string> DataReceived;
        event Action<Exception> ErrorReceived;
        event Action Completed;
        Task CancelAsync();
    }
}