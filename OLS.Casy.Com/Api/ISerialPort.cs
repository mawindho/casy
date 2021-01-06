using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OLS.Casy.Com.Api
{

    public interface ISerialPort : IDisposable
    {
        string PortName { get; set; }
        bool IsOpen { get; }
        int BaudRate { get; set; }
        Parity Parity { get; set; }
        int DataBits { get; set; }
        StopBits StopBits { get; set; }
        bool RtsEnable { get; set; }

        int BytesToRead { get; }

        void Open();
        void Read(byte[] buffer, int offset, int count);
        void Write(string text);
        void Write(byte[] buffer, int offset, int count);
        void DiscardInBuffer();
        void DiscardOutBuffer();
        void Close();

        event SerialDataReceivedEventHandler DataReceived;

        string[] GetPortNames();
    }
}
