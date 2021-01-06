using System;

namespace OLS.Casy.Ui.Base
{
    public class EventInformation<TEventArgs> : EventArgs where TEventArgs : EventArgs
    {
        private readonly object _commandParameter;
        private readonly TEventArgs _eventArgs;
        private readonly object _sender;

        public EventInformation(object sender, TEventArgs eventArgs, object commandParameter)
        {
            this._sender = sender;
            this._eventArgs = eventArgs;
            this._commandParameter = commandParameter;
        }

        public object Sender
        {
            get { return this._sender; }
        }

        public TEventArgs EventArgs
        {
            get { return this._eventArgs; }
        }

        public object CommandParameter
        {
            get { return this._commandParameter; }
        }
    }
}
