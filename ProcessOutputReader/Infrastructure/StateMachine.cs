using System.Runtime.CompilerServices;

namespace ProcessOutputReader.Infrastructure
{
    internal enum States
    {
        Undefined,
        Nothing,
        DataReceived,
        ErrorReceived,
        ReceivingStopped
    }

    internal class StateMachine
    {
        private bool _isDataReceiveStopped;
        private bool _isErrorReceiveStopped;

        public States State { get; private set; }

        public StateMachine()
        {
	        State = States.Undefined;
        }

        private void SetReceivingStopped()
        {
            State = _isDataReceiveStopped && _isErrorReceiveStopped
                ? States.ReceivingStopped
                : States.Nothing;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Token SetDataReceived()
        {
            State = _isDataReceiveStopped
                ? States.Nothing
                : States.DataReceived;

            return new Token(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Token SetErrorReceived()
        {
            State = _isErrorReceiveStopped
                ? States.Nothing
                : States.ErrorReceived;

            return new Token(this);
        }

        public void SetDataReceiveStopped()
        {
            _isDataReceiveStopped = true;

            SetReceivingStopped();
        }

        public void SetErrorReceiveStopped()
        {
            _isErrorReceiveStopped = true;

            SetReceivingStopped();
        }

        #region Nested types

        public readonly struct Token : IDisposable
        {
            private readonly StateMachine _machine;

            public Token(StateMachine machine)
            {
                _machine = machine;
            }

            public void Dispose()
            {
                _machine.State = States.Undefined;
            }
        }

        #endregion
    }
}