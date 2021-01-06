using System;

namespace OLS.Casy.IO.Api
{
    public class UsbStickDetectedEventArgs : EventArgs
    {
        public UsbStickDetectedEventArgs(string usbPath)
        {
            this.UsbPath = usbPath;
        }
        public string UsbPath { get; private set; }
    }

    public interface IUsbDetectionService
    {
        event EventHandler<UsbStickDetectedEventArgs> UsbStickDetectedEvent;
    }
}
