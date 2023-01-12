using System.Text;
using ProcessOutputReader.Interfaces.Infrastructure;

namespace ProcessOutputReader.Infrastructure.DataSources
{
    internal abstract class ProcessDataSourceBase
    {
        private readonly StringBuilder _errorBuilder;
        protected readonly StateMachine StateMachine;

        public event Action<ChangedEventArgs>? Changed;

        protected ProcessDataSourceBase()
        {
            _errorBuilder = new StringBuilder();
            StateMachine = new StateMachine();
        }

        protected async Task ReceiveHandlerAsync(string? value)
        {
            switch (StateMachine.State)
            {
                case States.DataReceived:
                    {
                        RaiseChanged(ChangedEventArgs.FromValue(value!));
                    }
                    break;

                case States.ErrorReceived:
                    {
                        _errorBuilder.Append(value);
                    }
                    break;

                case States.ReceivingStopped:
                    {
                        string? error = null;

                        if (_errorBuilder.Length != 0)
                        {
                            error = _errorBuilder.ToString();
                        }

                        await StopUpdateInternalAsync(error).ConfigureAwait(false);
                    }
                    break;

                case States.Undefined:
                    {
                        const string error = "Недопустимое состояние конечного автомата.";

                        await StopUpdateInternalAsync(error).ConfigureAwait(false);
                    }
                    break;

                case States.Nothing:
                    return;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async Task StopUpdateInternalAsync(string? error)
        {
            Exception? exception = null;

            try
            {
                await StopUpdateAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            var exceptions = new List<Exception>();

            if (exception != null)
            {
                exceptions.Add(exception);
            }

            if (error != null)
            {
                exceptions.Add(new InvalidOperationException(error));
            }

            RaiseChanged(exceptions.Count != 0
                ? ChangedEventArgs.FromError(new AggregateException(exceptions))
                : ChangedEventArgs.FromCompleted());
        }

        private void RaiseChanged(ChangedEventArgs args)
        {
            Volatile.Read(ref Changed)?.Invoke(args);
        }

        public abstract Task StopUpdateAsync();
    }
}