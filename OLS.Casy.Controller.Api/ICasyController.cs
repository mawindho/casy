using System;
using System.Threading.Tasks;

namespace OLS.Casy.Controller.Api
{
    public class IsConnectedChangedEventArgs : EventArgs
    {
        public IsConnectedChangedEventArgs(bool connectionState)
        {
            this.ConnectionState = connectionState;
        }
        public bool ConnectionState { get; private set; }
    }

    /// <summary>
    /// Controller implementation for general operations available for interaction with
    /// a casy device
    /// </summary>
    public interface ICasyController
    {
        /// <summary>
        /// Starts async a self test of the device
        /// </summary>
        /// <returns>Response string of the operation</returns>
        void StartSelfTest(bool doShowLoginScreen);

        string GetSerialNumber();

        bool SetSerialNumber(string serialNumber);

        event EventHandler<IsConnectedChangedEventArgs> OnIsConnectedChangedEvent;

        bool IsConnected { get; }
        bool ForceCheckIsConnected();
    }
}
