using Moq;
using OLS.Casy.Com.Api;
using OLS.Casy.Core;
using OLS.Casy.Models.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Reflection;
using System.Text;
using System.Threading;

namespace OLS.Casy.Test.Mock
{
    public class SerialPortMock : Mock<ISerialPort>
    {
        public static string CALIBDATA = "CASY C 00.66\u001a\0\0\u000f\0TTC-2BA-1015\0\0\0URS\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\013072007CASYC 2.33\0\0URS\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\013072007CASYCAL 2.35\0\u0096\u0002";

        private bool _isOpen;
        private byte[] _currentAnswer;
        private string _connectedPort;

        public SerialPortMock()
        {
            this._isOpen = false;
            this._currentAnswer = null;
            this.WrittenValues = new List<object>();
            this.ReadValues = new List<string>();

            Initialize();
        }

        public int ActiveCapillary { get; set; }
        public int ActiveToDiameter { get; set; }
        public List<object> WrittenValues { get; set; }
        public List<string> ReadValues { get; set; }

        private void Initialize()
        {
            this.Setup(serialPort => serialPort.GetPortNames()).Returns(new[] { "COM3" });
            this.Setup(serialPort => serialPort.DiscardInBuffer());
            this.Setup(serialPort => serialPort.DiscardOutBuffer());
            this.SetupAllProperties();
            this.SetupSet(serialPort => serialPort.PortName = It.IsAny<string>()).Callback(delegate (string s)
            {
                _connectedPort = s;
                if (s == "COM3")
                {
                    this._isOpen = true;
                }
            });
            this.SetupGet(serialPort => serialPort.PortName).Returns(() =>
            {
                return this._connectedPort;
            });
            this.SetupGet(serialPort => serialPort.IsOpen).Returns(() =>
            {
                return this._isOpen;
            });
            this.Setup(serialPort => serialPort.Write(It.IsAny<string>())).Callback(delegate (string text)
            {
                this.WrittenValues.Add(text);

                ConstructorInfo constructor = typeof(SerialDataReceivedEventArgs).GetConstructor(
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    new[] { typeof(SerialData) }, null);

                SerialDataReceivedEventArgs eventArgs =
                    (SerialDataReceivedEventArgs)constructor.Invoke(new object[] { SerialData.Eof });

                byte[] header = null;
                byte[] buffer;
                byte[] footer = System.Text.Encoding.Default.GetBytes("\n\r");

                text = text.Replace("\r", "");

                string command = text;
                string param = null;
                if (text.StartsWith("!"))
                {
                    command = text.Substring(1);
                }
                if (command.Contains("#"))
                {
                    var split = command.Split('#');
                    param = split[1];
                    command = split[0];
                }

                string answer = string.Format("!{0} OK\n\r", param == null ? command : string.Format("{0}#{1}", command, param));

                switch (command)
                {
                    //clean
                    case "01":
                    // self test
                    case "16":
                    // get error
                    case "24":
                    // start 200
                    case "26":
                    // start 400
                    case "27":
                    // clean waste
                    case "66":
                    // clean capillary
                    case "67":
                    // tightness
                    case "75":
                    // test pattern
                    case "7A":
                    // self test hw
                    case "80":
                    // self test sw
                    case "81":
                    // self test pressure
                    case "82":
                    // dry
                    case "83":
                        answer += string.Format("\"{0},0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000\n\r", command);
                        break;
                    // LED test
                    case "03":
                        header = System.Text.Encoding.Default.GetBytes(answer);
                        answer = null;

                        _currentAnswer = new byte[8 + 1 + 2];
                        Array.Copy(header, 0, _currentAnswer, 0, header.Length);

                        byte leds = 0;
                        leds += (int)LEDs.LightBarrier;
                        leds += (int)LEDs.FirstRed;

                        _currentAnswer[8] = leds;
                        Array.Copy(footer, 0, _currentAnswer, 9, footer.Length);
                        break;
                    // get vent status
                    case "1D":
                        header = System.Text.Encoding.Default.GetBytes(answer);
                        answer = null;

                        _currentAnswer = new byte[8 + 1 + 2];
                        Array.Copy(header, 0, _currentAnswer, 0, header.Length);

                        byte ventStatus = 0;
                        ventStatus += (int)Valves.Meas;
                        ventStatus += (int)Valves.PumpEngine;

                        _currentAnswer[8] = ventStatus;
                        Array.Copy(footer, 0, _currentAnswer, 9, footer.Length);
                        break;
                    // statistik
                    case "20":
                        buffer = new byte[2920];
                        header = System.Text.Encoding.Default.GetBytes(string.Format("{0}\"20,", answer));
                        answer = null;

                        _currentAnswer = new byte[512];
                        Array.Copy(header, 0, _currentAnswer, 0, header.Length);
                        Array.Copy(buffer, 0, _currentAnswer, 14, 498);

                        this.Raise(serialPort => serialPort.DataReceived += null, eventArgs);

                        Thread.Sleep(200);
                        _currentAnswer = new byte[512];
                        Array.Copy(buffer, 498, _currentAnswer, 0, 512);

                        this.Raise(serialPort => serialPort.DataReceived += null, eventArgs);

                        Thread.Sleep(200);
                        _currentAnswer = new byte[512];
                        Array.Copy(buffer, 1010, _currentAnswer, 0, 512);

                        this.Raise(serialPort => serialPort.DataReceived += null, eventArgs);

                        Thread.Sleep(200);
                        _currentAnswer = new byte[512];
                        Array.Copy(buffer, 1522, _currentAnswer, 0, 512);

                        this.Raise(serialPort => serialPort.DataReceived += null, eventArgs);

                        Thread.Sleep(200);
                        _currentAnswer = new byte[512];
                        Array.Copy(buffer, 2034, _currentAnswer, 0, 512);

                        this.Raise(serialPort => serialPort.DataReceived += null, eventArgs);

                        Thread.Sleep(200);
                        _currentAnswer = new byte[374 + 2];
                        Array.Copy(buffer, 2546, _currentAnswer, 0, 374);
                        Array.Copy(footer, 0, _currentAnswer, 374, footer.Length);

                        this.Raise(serialPort => serialPort.DataReceived += null, eventArgs);

                        Thread.Sleep(200);
                        _currentAnswer = new byte[] { };
                        break;
                    // get date time
                    case "51":
                        answer += "\"51,201804161448231234";
                        break;
                    // get serialnumber
                    case "53":
                        answer += "\"53,TT-123-1234    1234\n\r";
                        break;
                    // bindat cd
                    case "55":
                        buffer = new byte[2064];
                        header = System.Text.Encoding.Default.GetBytes(string.Format("{0}\"55,", answer));
                        answer = null;

                        _currentAnswer = new byte[512];
                        Array.Copy(header, 0, _currentAnswer, 0, header.Length);
                        var curDestIndex = buffer.Length - header.Length;
                        Array.Copy(buffer, 0, _currentAnswer, 14, 498);

                        this.Raise(serialPort => serialPort.DataReceived += null, eventArgs);

                        Thread.Sleep(200);
                        _currentAnswer = new byte[512];
                        Array.Copy(buffer, 498, _currentAnswer, 0, 512);

                        this.Raise(serialPort => serialPort.DataReceived += null, eventArgs);

                        Thread.Sleep(200);
                        _currentAnswer = new byte[512];
                        Array.Copy(buffer, 1010, _currentAnswer, 0, 512);

                        this.Raise(serialPort => serialPort.DataReceived += null, eventArgs);

                        Thread.Sleep(200);
                        _currentAnswer = new byte[512];
                        Array.Copy(buffer, 1522, _currentAnswer, 0, 512);

                        this.Raise(serialPort => serialPort.DataReceived += null, eventArgs);

                        Thread.Sleep(200);
                        _currentAnswer = new byte[32];
                        Array.Copy(buffer, 2034, _currentAnswer, 0, 30);
                        Array.Copy(footer, 0, _currentAnswer, 30, footer.Length);

                        this.Raise(serialPort => serialPort.DataReceived += null, eventArgs);

                        Thread.Sleep(200);
                        _currentAnswer = new byte[] { };
                        break;
                    // check calib
                    case "56":
                        buffer = new byte[12 + 8 + 2];

                        header = System.Text.Encoding.Default.GetBytes(string.Format("{0}\"56,", answer));
                        answer = null;
                        var toDiameterBytes = BitConverter.GetBytes((ushort)(this.ActiveToDiameter == 0 ? 15 : this.ActiveToDiameter));
                        var capillaryBytes = (BitConverter.GetBytes(SwapHelper.SwapBytes((ushort)(this.ActiveCapillary == 0 ? 60 : this.ActiveCapillary))));

                        //byte[] fillUp = new byte[15];
                        using (MemoryStream ms = new MemoryStream(buffer))
                        using (BinaryWriter sw = new BinaryWriter(ms, Encoding.ASCII))
                        {
                            sw.Write(header);
                            //sw.Write(fillUp);
                            sw.Write(toDiameterBytes);
                            sw.Write(capillaryBytes);
                            
                            //fillUp = new byte[105];
                            sw.Write(new byte[4]);
                            sw.Write(footer);
                        }

                        _currentAnswer = buffer;
                        break;
                    // get header
                    case "5D":
                        buffer = new byte[158];
                        byte[] checksum = Encoding.Default.GetBytes("1234");
                        header = System.Text.Encoding.Default.GetBytes(string.Format("{0}\"5D,", answer));
                        answer = null;

                        _currentAnswer = new byte[176];

                        Array.Copy(header, 0, _currentAnswer, 0, header.Length);
                        Array.Copy(buffer, 0, _currentAnswer, 12, buffer.Length);
                        Array.Copy(checksum, 0, _currentAnswer, 170, checksum.Length);
                        Array.Copy(footer, 0, _currentAnswer, 174, footer.Length);
                        break;
                    // req cs
                    case "61":
                        header = System.Text.Encoding.Default.GetBytes(string.Format("{0}\"61,", answer));
                        answer = null;
                        buffer = BitConverter.GetBytes((uint)1234);

                        _currentAnswer = new byte[12 + 2 + 4]; //System.Text.Encoding.Default.GetBytes("!61 OK\n\r\"61," + BitConverter.GetBytes((uint)1234) + "\n\r");
                        Array.Copy(header, 0, _currentAnswer, 0, header.Length);
                        Array.Copy(buffer, 0, _currentAnswer, 12, buffer.Length);
                        Array.Copy(footer, 0, _currentAnswer, 12 + buffer.Length, footer.Length);
                        break;
                    // clean num
                    case "62":
                        answer += string.Format("\"01,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000\n\r", command);
                        break;
                    // risetime
                    case "76":
                        answer += "\"76,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000\n\r";
                        break;
                    // calibrate
                    case "CALIBTTC":
                        answer = "CALIBTTC OK\n\r";
                        break;
                    // master pin
                    case "MASTERPIN":
                        answer = string.Format("MASTERPIN#{0} OK\n\r", param);
                        break;
                    case "GETCAPVLT":
                        answer = "GETCAPVLT OK\n\r45.9\n\r";
                        break;
                    case "GETPRESSURE":
                        answer = "GETPRESSURE OK\n\r4.2\n\r";
                        break;
                    case "INFO ON":
                        answer = "INFO ON OK\n\r";
                        break;
                }

                if (!string.IsNullOrEmpty(answer))
                {
                    _currentAnswer = System.Text.Encoding.Default.GetBytes(answer);
                }

                this.Raise(serialPort => serialPort.DataReceived += null, eventArgs);
            });
            this.Setup(serialPort => serialPort.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>())).Callback(delegate (byte[] buffer, int offset, int count)
            {
                this.WrittenValues.Add(buffer);
                _currentAnswer = System.Text.Encoding.Default.GetBytes("\"10,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000\n\r");

                this.ExtractCalibrationInfo(buffer);

                ConstructorInfo constructor = typeof(SerialDataReceivedEventArgs).GetConstructor(
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    new[] { typeof(SerialData) }, null);

                SerialDataReceivedEventArgs eventArgs =
                    (SerialDataReceivedEventArgs)constructor.Invoke(new object[] { SerialData.Eof });

                this.Raise(serialPort => serialPort.DataReceived += null, eventArgs);
            });
            this.Setup(serialPort => serialPort.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>())).Callback(delegate (byte[] buffer, int offset, int count)
            {
                this.ReadValues.Add(System.Text.Encoding.Default.GetString(_currentAnswer));
                Array.Copy(_currentAnswer, 0, buffer, 0, _currentAnswer.Length);
            });
            this.SetupGet(serialPort => serialPort.BytesToRead).Returns(() =>
            {
                return this._currentAnswer == null ? 0 : this._currentAnswer.Length;
            });
        }

        private void ExtractCalibrationInfo(byte[] calibrationData)
        {
            MemoryStream ms = new MemoryStream(calibrationData);

            //byte[] buffer = new byte[15];
            ////ms.Read(buffer, 0, 105);
            //ms.Read(buffer, 0, 15);

            //byte[] wToDiameterBuf = new byte[2];
            //ms.Read(wToDiameterBuf, 0, 2);
            //this.ActiveToDiameter = BitConverter.ToUInt16(wToDiameterBuf, 0);

            var buffer = new byte[105];
            ms.Read(buffer, 0, 105);
            //buffer = new byte[105];
            //ms.Read(buffer, 0, 105);

            byte[] wCapillarySizeBuf = new byte[2];
            ms.Read(wCapillarySizeBuf, 0, 2);
            this.ActiveCapillary = SwapHelper.SwapBytes(BitConverter.ToUInt16(wCapillarySizeBuf, 0));

            ms.Close();
        }
    }
}
