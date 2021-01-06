using OLS.Casy.Com.Api;
using System.ComponentModel.Composition;
using System.IO.Ports;

namespace OLS.Casy.Com
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(ISerialPort))]
    public class SerialPortWrapper : ISerialPort
    {
        private SerialPort _serialPort;

        [ImportingConstructor]
        public SerialPortWrapper()
        {
            this._serialPort = new SerialPort();
        }

        public string PortName
        {
            get { return this._serialPort.PortName; }
            set { this._serialPort.PortName = value; }
        }

        public bool IsOpen
        {
            get { return this._serialPort.IsOpen; }
        }
        public int BaudRate
        {
            get { return this._serialPort.BaudRate; }
            set { this._serialPort.BaudRate = value; }
        }

        public Parity Parity
        {
            get { return this._serialPort.Parity; }
            set { this._serialPort.Parity = value; }
        }

        public int DataBits
        {
            get { return this._serialPort.DataBits; }
            set { this._serialPort.DataBits = value; }
        }

        public StopBits StopBits
        {
            get { return this._serialPort.StopBits; }
            set { this._serialPort.StopBits = value; }
        }
        public bool RtsEnable
        {
            get { return this._serialPort.RtsEnable; }
            set { this._serialPort.RtsEnable = value; }
        }

        public int BytesToRead
        {
            get { return this._serialPort.BytesToRead; }
        }

        public event SerialDataReceivedEventHandler DataReceived
        {
            add { this._serialPort.DataReceived += value; }
            remove { this._serialPort.DataReceived -= value; }
        }

        public void Open()
        {
            this._serialPort.Open();
        }

        public void Read(byte[] buffer, int offset, int count)
        {
            this._serialPort.Read(buffer, offset, count);
        }

        public void Write(string text)
        {
            if (_serialPort.IsOpen)
            {
                this._serialPort.Write(text);
            }
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            if (_serialPort.IsOpen)
            {
                this._serialPort.Write(buffer, offset, count);
            }
        }

        public void DiscardInBuffer()
        {
            this._serialPort.DiscardInBuffer();
        }

        public void DiscardOutBuffer()
        {
            this._serialPort.DiscardOutBuffer();
        }

        public void Close()
        {
            this._serialPort.Close();
        }

        public void Dispose()
        {
            this._serialPort.Dispose();
        }

        public string[] GetPortNames()
        {
            return SerialPort.GetPortNames();
        }
    }
}
