namespace ProcessOutputReader.Interfaces.Infrastructure
{
    internal interface IWorkToken
    {
        event Action<string> DataReceived;
        event Action<Exception> ErrorReceived;
        event Action Completed;
        Task CancelAsync();
    }
}