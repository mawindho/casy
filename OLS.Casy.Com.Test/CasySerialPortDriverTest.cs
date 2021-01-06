using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using OLS.Casy.Core;
using OLS.Casy.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace OLS.Casy.Com.Test
{
    [TestClass]
    public class CasySerialPortDriverTest
    {
        //private SerialPortMock _serialPortMock;
        private Mock<IProgress<string>> _progressMock;

        //private ManualResetEvent _provider.ConnectedAwaiter;
        private MockedCasySerialPortDriverProvider _provider;

        [TestInitialize]
        public void Initialize()
        {
            //this._provider.SerialPortMock = Casy.Test.Mock.Mocks.SerialPortMock;
            
            this._progressMock = new Mock<IProgress<string>>();
            this._progressMock.Setup(progress => progress.Report(It.IsAny<string>()));

            _provider = new MockedCasySerialPortDriverProvider(Casy.Test.Mock.Mocks.SerialPortMock);
            //this._provider.ConnectedAwaiter = new ManualResetEvent(false);
        }

        [TestCleanup]
        public void CleanUp()
        {
            this._provider = null;
            this._progressMock = null;
        }

        [TestMethod]
        public void PrepareDriver_Test()
        {
            var casySerialPortDriver = _provider.CasySerialPortDriver;

            // Called in OnImportsSatisfied
            this._provider.SerialPortMock.VerifySet(serialPort => serialPort.BaudRate = 19200);
            this._provider.SerialPortMock.VerifySet(serialPort => serialPort.Parity = System.IO.Ports.Parity.None);
            this._provider.SerialPortMock.VerifySet(serialPort => serialPort.DataBits = 8);
            this._provider.SerialPortMock.VerifySet(serialPort => serialPort.StopBits = System.IO.Ports.StopBits.One);
            this._provider.SerialPortMock.VerifySet(serialPort => serialPort.RtsEnable = true);

            int progressCount = 0;

            this._progressMock.Setup(progress => progress.Report(It.IsAny<string>())).Callback(delegate (string s)
            {
                progressCount++;
            });

            casySerialPortDriver.Prepare(this._progressMock.Object);

            this._provider.ConnectedAwaiter.WaitOne(60000);

            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.Verify(serialPort => serialPort.GetPortNames(), Times.Exactly(1));
            //this._provider.SerialPortMock.Verify(serialPort => serialPort.Close());
            //this._provider.SerialPortMock.Verify(serialPort => serialPort.Dispose());
            this._provider.SerialPortMock.Verify(serialPort => serialPort.Open(), Times.Exactly(1));
            this._provider.SerialPortMock.Verify(serialPort => serialPort.IsOpen, Times.Exactly(3));
            this._provider.SerialPortMock.VerifySet(serialPort => serialPort.PortName = It.IsIn<string>(new[] { "COM3" }), Times.Exactly(1));
            Assert.AreEqual("COM3", casySerialPortDriver.ConnectedSerialPort);
            Assert.AreEqual(2, progressCount);

            var serialNumberTuple = casySerialPortDriver.GetSerialNumber(this._progressMock.Object);
            Assert.AreEqual("TT-123-1234", serialNumberTuple.Item1);

            casySerialPortDriver.Deinitialize(this._progressMock.Object);
        }

        [TestMethod]
        public void CheckCasyDeviceConnection_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();

            //Check normal behavior with two COM Ports, first not connected, second connected
            this._provider.SerialPortMock.Setup(serialPort => serialPort.GetPortNames()).Returns(new[] { "COM1", "COM3" });

            casySerialPortDriver.Prepare(this._progressMock.Object);

            _provider.ConnectedAwaiter.WaitOne(60000);

            Assert.IsTrue(casySerialPortDriver.IsConnected);


            //Check no COM ports found
            this._provider.SerialPortMock.Setup(serialPort => serialPort.GetPortNames()).Returns(new string[] { });

            ManualResetEvent notConnectedAwaiter = new ManualResetEvent(false);

            casySerialPortDriver.OnIsConnectedChangedEvent += (s, e) =>
            {
                if (!casySerialPortDriver.IsConnected)
                {
                    notConnectedAwaiter.Set();
                }
            };

            casySerialPortDriver.CheckCasyDeviceConnection();
            notConnectedAwaiter.WaitOne(60000);
            Assert.IsFalse(casySerialPortDriver.IsConnected);

            casySerialPortDriver.Deinitialize(this._progressMock.Object);
        }

        [TestMethod]
        public void Clean_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            bool exceptionRaised = false;

            // Test wrong clean count
            try
            {
                casySerialPortDriver.Clean(this._progressMock.Object, 0);
            }
            catch(ArgumentException e)
            {
                StringAssert.Equals("cleanCount", e.ParamName);
                exceptionRaised = true;
            }

            if(!exceptionRaised)
            {
                Assert.Fail("No exception was thrown");
            }

            // Test 1 clean count
            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();

            var cleanResult = casySerialPortDriver.Clean(this._progressMock.Object, 1);

            Assert.AreEqual("0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000", cleanResult);
            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!01\r");
            Assert.AreEqual(1, this._provider.SerialPortMock.ReadValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.ReadValues, "!01 OK\n\r\"01,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000\n\r");

            // Test 2+ clean count
            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();

            cleanResult = casySerialPortDriver.Clean(this._progressMock.Object, 2);

            Assert.AreEqual("0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000", cleanResult);
            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!62#2\r");
            Assert.AreEqual(1, this._provider.SerialPortMock.ReadValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.ReadValues, "!62#2 OK\n\r\"01,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000\n\r");

            casySerialPortDriver.Deinitialize(this._progressMock.Object);
        }

        [TestMethod]
        public void CleanWaste_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();

            var cleanWasteResult = casySerialPortDriver.CleanWaste(this._progressMock.Object);

            Assert.AreEqual("0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000", cleanWasteResult);
            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!66\r");
            Assert.AreEqual(1, this._provider.SerialPortMock.ReadValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.ReadValues, "!66 OK\n\r\"66,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000\n\r");
        }

        [TestMethod]
        public void CleanCapillary_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();

            var cleanCapillaryResult = casySerialPortDriver.CleanCapillary(this._progressMock.Object);

            Assert.AreEqual("0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000", cleanCapillaryResult);
            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!67\r");
            Assert.AreEqual(1, this._provider.SerialPortMock.ReadValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.ReadValues, "!67 OK\n\r\"67,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000\n\r");
        }

        [TestMethod]
        public void StartSelfTest_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();

            var startSelfTestResult = casySerialPortDriver.StartSelfTest(this._progressMock.Object);

            Assert.AreEqual("0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000", startSelfTestResult);
            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!16\r");
            Assert.AreEqual(1, this._provider.SerialPortMock.ReadValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.ReadValues, "!16 OK\n\r\"16,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000\n\r");
        }

        [TestMethod]
        public void StartHardwareSelfTest_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();

            var startHardwareSelfTestResult = casySerialPortDriver.StartHardwareSelfTest(this._progressMock.Object);

            Assert.AreEqual("0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000", startHardwareSelfTestResult);
            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!80\r");
            Assert.AreEqual(1, this._provider.SerialPortMock.ReadValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.ReadValues, "!80 OK\n\r\"80,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000\n\r");
        }

        [TestMethod]
        public void StartSoftwareSelfTest_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();

            var startSoftwareSelfTestResult = casySerialPortDriver.StartSoftwareSelfTest(this._progressMock.Object);

            Assert.AreEqual("0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000", startSoftwareSelfTestResult);
            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!81\r");
            Assert.AreEqual(1, this._provider.SerialPortMock.ReadValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.ReadValues, "!81 OK\n\r\"81,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000\n\r");
        }

        [TestMethod]
        public void StartPressureSelfTest_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();

            var startPressureSelfTestResult = casySerialPortDriver.StartPressureSystemSelfTest(this._progressMock.Object);

            Assert.AreEqual("0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000", startPressureSelfTestResult);
            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!82\r");
            Assert.AreEqual(1, this._provider.SerialPortMock.ReadValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.ReadValues, "!82 OK\n\r\"82,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000\n\r");
        }

        [TestMethod]
        public void GetError_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();

            var getErrorResult = casySerialPortDriver.GetError(this._progressMock.Object);

            Assert.AreEqual("0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000", getErrorResult);
            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!24\r");
            Assert.AreEqual(1, this._provider.SerialPortMock.ReadValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.ReadValues, "!24 OK\n\r\"24,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000\n\r");
        }

        [TestMethod]
        public void Calibrate_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            bool exceptionRaised = false;

            // Test wrong clean count
            try
            {
                casySerialPortDriver.Calibrate(0, null, this._progressMock.Object);
            }
            catch (ArgumentNullException e)
            {
                StringAssert.Equals("calibrationData", e.ParamName);
                exceptionRaised = true;
            }

            if (!exceptionRaised)
            {
                Assert.Fail("No exception was thrown");
            }

            // Test valid data
            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();

            byte[] calibData = System.Text.Encoding.Default.GetBytes(Casy.Test.Mock.SerialPortMock.CALIBDATA);

            var calibrationResult = casySerialPortDriver.Calibrate(20, calibData, this._progressMock.Object);

            Assert.AreEqual("0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000", calibrationResult);
            Assert.AreEqual(2, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "CALIBTTC\r");
            Assert.AreEqual(2, this._provider.SerialPortMock.ReadValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.ReadValues, "CALIBTTC OK\n\r");
        }

        [TestMethod]
        public void GetCalibrationVerifactionData_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();

            var getCalibrationVerifactionDataResult = casySerialPortDriver.GetCalibrationVerifactionData(this._progressMock.Object);

            Assert.AreEqual(getCalibrationVerifactionDataResult.Item1, (ushort) 15);
            Assert.AreEqual(SwapHelper.SwapBytes(getCalibrationVerifactionDataResult.Item2), (ushort) 60);
            Assert.AreEqual(getCalibrationVerifactionDataResult.Item3, (uint) 0);
            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!56\r");
            Assert.AreEqual(1, this._provider.SerialPortMock.ReadValues.Count);
            Assert.IsTrue(this._provider.SerialPortMock.ReadValues.Any(s => s.StartsWith("!56 OK\n\r\"56,")));
        }

        [TestMethod]
        public void Measure200_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();

            var measure200Result = casySerialPortDriver.Measure200(this._progressMock.Object);

            Assert.AreEqual("0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000", measure200Result);
            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!26\r");
            Assert.AreEqual(1, this._provider.SerialPortMock.ReadValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.ReadValues, "!26 OK\n\r\"26,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000\n\r");
        }

        public void Measure400_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();
            
            var measure400Result = casySerialPortDriver.Measure400(this._progressMock.Object);

            Assert.AreEqual("0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000", measure400Result);
            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!27\r");
            Assert.AreEqual(1, this._provider.SerialPortMock.ReadValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.ReadValues, "!27 OK\n\r\"27,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000\n\r");
        }

        [TestMethod]
        public void Stop_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();

            casySerialPortDriver.Stop(this._progressMock.Object);

            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!FF\r");
            Assert.AreEqual(1, this._provider.SerialPortMock.ReadValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.ReadValues, "!FF OK\n\r");
        }

        [TestMethod]
        public void GetBinaryData_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();

            var getBinaryDataResult = casySerialPortDriver.GetBinaryData(this._progressMock.Object);

            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!55#0\r");
            Assert.AreEqual(6, this._provider.SerialPortMock.ReadValues.Count);
            Assert.IsTrue(this._provider.SerialPortMock.ReadValues.Any(s => s.StartsWith("!55#0 OK\n\r")));
        }

        [TestMethod]
        public void GetDateTime_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();

            var getDateTimeResult = casySerialPortDriver.GetDateTime(this._progressMock.Object);

            Assert.AreEqual(getDateTimeResult.Item1, new DateTime(2018, 4, 16, 14, 48, 23));
            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!51\r");
            Assert.AreEqual(1, this._provider.SerialPortMock.ReadValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.ReadValues, "!51 OK\n\r\"51,201804161448231234");
        }

        [TestMethod]
        public void VerifyMasterPin_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();
            
            var verifyMasterPinResult = casySerialPortDriver.VerifyMasterPin(null, this._progressMock.Object);

            Assert.IsFalse(verifyMasterPinResult);
            
            verifyMasterPinResult = casySerialPortDriver.VerifyMasterPin(string.Empty, this._progressMock.Object);

            Assert.IsFalse(verifyMasterPinResult);
            
            verifyMasterPinResult = casySerialPortDriver.VerifyMasterPin("12345", this._progressMock.Object);

            Assert.IsTrue(verifyMasterPinResult);
            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "MASTERPIN#12345\r");
            Assert.AreEqual(1, this._provider.SerialPortMock.ReadValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.ReadValues, "MASTERPIN#12345 OK\n\r");
        }

        [TestMethod]
        public void GetHeader_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();
            
            var getHeaderResult = casySerialPortDriver.GetHeader(this._progressMock.Object);

            Assert.AreEqual(getHeaderResult.Item1.Length, 158);
            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!5D\r");
            Assert.AreEqual(1, this._provider.SerialPortMock.ReadValues.Count);
            Assert.IsTrue(this._provider.SerialPortMock.ReadValues.Any(s => s.StartsWith("!5D OK\n\r\"5D,")));
        }

        [TestMethod]
        public void RequestLastChecksum_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();
            
            var requestLastChecksumResult = casySerialPortDriver.RequestLastChecksum(this._progressMock.Object);

            Assert.AreEqual((uint) 1234, requestLastChecksumResult);
            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!61\r");
            Assert.AreEqual(1, this._provider.SerialPortMock.ReadValues.Count);
            Assert.IsTrue(this._provider.SerialPortMock.ReadValues.Any(s => s.StartsWith("!61 OK\n\r\"61,")));
        }

        [TestMethod]
        public void CreateTestPattern_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();
            
            var createTestPatternResult = casySerialPortDriver.CreateTestPattern(this._progressMock.Object);

            Assert.AreEqual("0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000", createTestPatternResult);
            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!7A\r");
            Assert.AreEqual(1, this._provider.SerialPortMock.ReadValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.ReadValues, "!7A OK\n\r\"7A,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000\n\r");
        }

        [TestMethod]
        public void Dry_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();
            
            var dryResult = casySerialPortDriver.Dry(this._progressMock.Object);

            Assert.AreEqual("0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000", dryResult);
            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!83\r");
            Assert.AreEqual(1, this._provider.SerialPortMock.ReadValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.ReadValues, "!83 OK\n\r\"83,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000\n\r");
        }

        [TestMethod]
        public void StartLEDTest_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();
            
            var startLEDTestResult = casySerialPortDriver.StartLEDTest(this._progressMock.Object);

            List<LEDs> leds = new List<LEDs>();
            for (int i = 0; i < 8; i++)
            {
                if ((startLEDTestResult & (1 << i)) != 0)
                {
                    leds.Add((LEDs)(1 << i));
                }
            }

            CollectionAssert.Contains(leds, LEDs.LightBarrier);
            CollectionAssert.Contains(leds, LEDs.FirstRed);
            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!03\r");
            Assert.AreEqual(1, this._provider.SerialPortMock.ReadValues.Count);
            Assert.IsTrue(this._provider.SerialPortMock.ReadValues.Any(s => s.StartsWith("!03 OK\n\r")));
        }

        [TestMethod]
        public void PerformBlow_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();
            
            var performBlowResult = casySerialPortDriver.PerformBlow(this._progressMock.Object);

            Assert.IsTrue(performBlowResult);
            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!04\r");
            Assert.AreEqual(1, this._provider.SerialPortMock.ReadValues.Count);
            Assert.IsTrue(this._provider.SerialPortMock.ReadValues.Any(s => s.StartsWith("!04 OK\n\r")));
        }

        [TestMethod]
        public void PerformSuck_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();
            
            var performSuckResult = casySerialPortDriver.PerformSuck(this._progressMock.Object);

            Assert.IsTrue(performSuckResult);
            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!05\r");
            Assert.AreEqual(1, this._provider.SerialPortMock.ReadValues.Count);
            Assert.IsTrue(this._provider.SerialPortMock.ReadValues.Any(s => s.StartsWith("!05 OK\n\r")));
        }

        [TestMethod]
        public void SetVacuumVentilState_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();
            
            var setVacuumVentilStateResult = casySerialPortDriver.SetVacuumVentilState(true, this._progressMock.Object);

            Assert.IsTrue(setVacuumVentilStateResult);
            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!06#1\r");
            Assert.AreEqual(1, this._provider.SerialPortMock.ReadValues.Count);
            Assert.IsTrue(this._provider.SerialPortMock.ReadValues.Any(s => s.StartsWith("!06#1 OK\n\r")));
            
            setVacuumVentilStateResult = casySerialPortDriver.SetVacuumVentilState(false, this._progressMock.Object);

            Assert.IsTrue(setVacuumVentilStateResult);
            Assert.AreEqual(2, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!06#0\r");
            Assert.AreEqual(2, this._provider.SerialPortMock.ReadValues.Count);
            Assert.IsTrue(this._provider.SerialPortMock.ReadValues.Any(s => s.StartsWith("!06#0 OK\n\r")));
        }

        [TestMethod]
        public void SetPumpEngineState_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();
            
            var setPumpEngineStateResult = casySerialPortDriver.SetPumpEngineState(true, this._progressMock.Object);

            Assert.IsTrue(setPumpEngineStateResult);
            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!07#1\r");
            Assert.AreEqual(1, this._provider.SerialPortMock.ReadValues.Count);
            Assert.IsTrue(this._provider.SerialPortMock.ReadValues.Any(s => s.StartsWith("!07#1 OK\n\r")));
            
            setPumpEngineStateResult = casySerialPortDriver.SetPumpEngineState(false, this._progressMock.Object);

            Assert.IsTrue(setPumpEngineStateResult);
            Assert.AreEqual(2, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!07#0\r");
            Assert.AreEqual(2, this._provider.SerialPortMock.ReadValues.Count);
            Assert.IsTrue(this._provider.SerialPortMock.ReadValues.Any(s => s.StartsWith("!07#0 OK\n\r")));
        }

        [TestMethod]
        public void SetCapillaryRelayVoltage_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();
            
            var setCapillaryRelayVoltageResult = casySerialPortDriver.SetCapillaryRelayVoltage(true, this._progressMock.Object);

            Assert.IsTrue(setCapillaryRelayVoltageResult);
            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!08#1\r");
            Assert.AreEqual(1, this._provider.SerialPortMock.ReadValues.Count);
            Assert.IsTrue(this._provider.SerialPortMock.ReadValues.Any(s => s.StartsWith("!08#1 OK\n\r")));
            
            setCapillaryRelayVoltageResult = casySerialPortDriver.SetCapillaryRelayVoltage(false, this._progressMock.Object);

            Assert.IsTrue(setCapillaryRelayVoltageResult);
            Assert.AreEqual(2, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!08#0\r");
            Assert.AreEqual(2, this._provider.SerialPortMock.ReadValues.Count);
            Assert.IsTrue(this._provider.SerialPortMock.ReadValues.Any(s => s.StartsWith("!08#0 OK\n\r")));
        }

        [TestMethod]
        public void SetMeasValveRelayVoltage_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();
            
            var setMeasValveRelayVoltageResult = casySerialPortDriver.SetMeasValveRelayVoltage(true, this._progressMock.Object);

            Assert.IsTrue(setMeasValveRelayVoltageResult);
            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!19#1\r");
            Assert.AreEqual(1, this._provider.SerialPortMock.ReadValues.Count);
            Assert.IsTrue(this._provider.SerialPortMock.ReadValues.Any(s => s.StartsWith("!19#1 OK\n\r")));

            setMeasValveRelayVoltageResult = casySerialPortDriver.SetMeasValveRelayVoltage(false, this._progressMock.Object);

            Assert.IsTrue(setMeasValveRelayVoltageResult);
            Assert.AreEqual(2, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!19#0\r");
            Assert.AreEqual(2, this._provider.SerialPortMock.ReadValues.Count);
            Assert.IsTrue(this._provider.SerialPortMock.ReadValues.Any(s => s.StartsWith("!19#0 OK\n\r")));
        }

        [TestMethod]
        public void SetWasteValveRelayVoltage_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();
            
            var setWasteValveRelayVoltageResult = casySerialPortDriver.SetWasteValveRelayVoltage(true, this._progressMock.Object);

            Assert.IsTrue(setWasteValveRelayVoltageResult);
            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!1C#1\r");
            Assert.AreEqual(1, this._provider.SerialPortMock.ReadValues.Count);
            Assert.IsTrue(this._provider.SerialPortMock.ReadValues.Any(s => s.StartsWith("!1C#1 OK\n\r")));
            
            setWasteValveRelayVoltageResult = casySerialPortDriver.SetWasteValveRelayVoltage(false, this._progressMock.Object);

            Assert.IsTrue(setWasteValveRelayVoltageResult);
            Assert.AreEqual(2, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!1C#0\r");
            Assert.AreEqual(2, this._provider.SerialPortMock.ReadValues.Count);
            Assert.IsTrue(this._provider.SerialPortMock.ReadValues.Any(s => s.StartsWith("!1C#0 OK\n\r")));
        }

        [TestMethod]
        public void GetValveState_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();
            
            var getValveStateResult = casySerialPortDriver.GetValveState(this._progressMock.Object);

            Dictionary<Valves, bool> valves = new Dictionary<Valves, bool>();
            for (int i = 0; i < 8; i++)
            {
                valves.Add((Valves)(1 << i), (getValveStateResult & (1 << i)) != 0);
            }

            Assert.IsTrue(valves[Valves.Meas]);
            Assert.IsTrue(valves[Valves.PumpEngine]);
            Assert.IsFalse(valves[Valves.Blow]);
            Assert.IsFalse(valves[Valves.Capillary]);
            Assert.IsFalse(valves[Valves.Clean]);
            Assert.IsFalse(valves[Valves.Suck]);
            Assert.IsFalse(valves[Valves.Vacuum]);
            Assert.IsFalse(valves[Valves.Waste]);

            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!1D\r");
            Assert.AreEqual(1, this._provider.SerialPortMock.ReadValues.Count);
            Assert.IsTrue(this._provider.SerialPortMock.ReadValues.Any(s => s.StartsWith("!1D OK\n\r")));
        }

        [TestMethod]
        public void GetStatistik_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();

            var getStatistikResult = casySerialPortDriver.GetStatistik(this._progressMock.Object);

            Assert.AreEqual(getStatistikResult.Length, 2924);
            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!20\r");
            Assert.AreEqual(7, this._provider.SerialPortMock.ReadValues.Count);
            Assert.IsTrue(this._provider.SerialPortMock.ReadValues.Any(s => s.StartsWith("!20 OK\n\r\"20,")));
        }

        [TestMethod]
        public void SetCleanValveRelayVoltage_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();
            
            var setCleanValveRelayVoltageResult = casySerialPortDriver.SetCleanValveRelayVoltage(true, this._progressMock.Object);

            Assert.IsTrue(setCleanValveRelayVoltageResult);
            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!21#1\r");
            Assert.AreEqual(1, this._provider.SerialPortMock.ReadValues.Count);
            Assert.IsTrue(this._provider.SerialPortMock.ReadValues.Any(s => s.StartsWith("!21#1 OK\n\r")));
            
            setCleanValveRelayVoltageResult = casySerialPortDriver.SetCleanValveRelayVoltage(false, this._progressMock.Object);

            Assert.IsTrue(setCleanValveRelayVoltageResult);
            Assert.AreEqual(2, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!21#0\r");
            Assert.AreEqual(2, this._provider.SerialPortMock.ReadValues.Count);
            Assert.IsTrue(this._provider.SerialPortMock.ReadValues.Any(s => s.StartsWith("!21#0 OK\n\r")));
        }

        [TestMethod]
        public void SetBlowValveRelayVoltage_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();
            
            var setBlowValveRelayVoltageResult = casySerialPortDriver.SetBlowValveRelayVoltage(true, this._progressMock.Object);

            Assert.IsTrue(setBlowValveRelayVoltageResult);
            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!34#1\r");
            Assert.AreEqual(1, this._provider.SerialPortMock.ReadValues.Count);
            Assert.IsTrue(this._provider.SerialPortMock.ReadValues.Any(s => s.StartsWith("!34#1 OK\n\r")));

            setBlowValveRelayVoltageResult = casySerialPortDriver.SetBlowValveRelayVoltage(false, this._progressMock.Object);

            Assert.IsTrue(setBlowValveRelayVoltageResult);
            Assert.AreEqual(2, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!34#0\r");
            Assert.AreEqual(2, this._provider.SerialPortMock.ReadValues.Count);
            Assert.IsTrue(this._provider.SerialPortMock.ReadValues.Any(s => s.StartsWith("!34#0 OK\n\r")));
        }

        [TestMethod]
        public void SetSuckValveRelayVoltage_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();
            
            var setSuckValveRelayVoltageResult = casySerialPortDriver.SetSuckValveRelayVoltage(true, this._progressMock.Object);

            Assert.IsTrue(setSuckValveRelayVoltageResult);
            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!36#1\r");
            Assert.AreEqual(1, this._provider.SerialPortMock.ReadValues.Count);
            Assert.IsTrue(this._provider.SerialPortMock.ReadValues.Any(s => s.StartsWith("!36#1 OK\n\r")));

            setSuckValveRelayVoltageResult = casySerialPortDriver.SetSuckValveRelayVoltage(false, this._progressMock.Object);

            Assert.IsTrue(setSuckValveRelayVoltageResult);
            Assert.AreEqual(2, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!36#0\r");
            Assert.AreEqual(2, this._provider.SerialPortMock.ReadValues.Count);
            Assert.IsTrue(this._provider.SerialPortMock.ReadValues.Any(s => s.StartsWith("!36#0 OK\n\r")));
        }

        [TestMethod]
        public void SetCapillaryVoltage_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();
            
            var setCapillaryVoltageResult = casySerialPortDriver.SetCapillaryVoltage(219, this._progressMock.Object);

            Assert.IsFalse(setCapillaryVoltageResult);
            Assert.AreEqual(0, this._provider.SerialPortMock.WrittenValues.Count);
            Assert.AreEqual(0, this._provider.SerialPortMock.ReadValues.Count);
            
            setCapillaryVoltageResult = casySerialPortDriver.SetCapillaryVoltage(256, this._progressMock.Object);

            Assert.IsFalse(setCapillaryVoltageResult);
            Assert.AreEqual(0, this._provider.SerialPortMock.WrittenValues.Count);
            Assert.AreEqual(0, this._provider.SerialPortMock.ReadValues.Count);
            
            setCapillaryVoltageResult = casySerialPortDriver.SetCapillaryVoltage(242, this._progressMock.Object);

            Assert.IsTrue(setCapillaryVoltageResult);
            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!2A#242\r");
            Assert.AreEqual(1, this._provider.SerialPortMock.ReadValues.Count);
            Assert.IsTrue(this._provider.SerialPortMock.ReadValues.Any(s => s.StartsWith("!2A#242 OK\n\r")));
        }

        [TestMethod]
        public void GetCapillaryVoltage_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();
            
            var getCapillaryVoltageResult = casySerialPortDriver.GetCapillaryVoltage(this._progressMock.Object);

            Assert.AreEqual(getCapillaryVoltageResult, 45.9d);
            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "GETCAPVLT\r");
            Assert.AreEqual(1, this._provider.SerialPortMock.ReadValues.Count);
            Assert.IsTrue(this._provider.SerialPortMock.ReadValues.Any(s => s.StartsWith("GETCAPVLT OK\n\r")));
        }

        [TestMethod]
        public void GetPressure_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();
            
            var getPressureResult = casySerialPortDriver.GetPressure(this._progressMock.Object);

            Assert.AreEqual(getPressureResult, 4.2d);
            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "GETPRESSURE\r");
            Assert.AreEqual(1, this._provider.SerialPortMock.ReadValues.Count);
            Assert.IsTrue(this._provider.SerialPortMock.ReadValues.Any(s => s.StartsWith("GETPRESSURE OK\n\r")));
        }

        [TestMethod]
        public void ClearErrorBytes_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();
            
            var clearErrorBytesResult = casySerialPortDriver.ClearErrorBytes(this._progressMock.Object);

            Assert.IsTrue(clearErrorBytesResult);
            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!65\r");
            Assert.AreEqual(1, this._provider.SerialPortMock.ReadValues.Count);
            Assert.IsTrue(this._provider.SerialPortMock.ReadValues.Any(s => s.StartsWith("!65 OK\n\r")));
        }

        [TestMethod]
        public void ResetStatistic_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();
            
            var resetStatisticResult = casySerialPortDriver.ResetStatistic(this._progressMock.Object);

            Assert.IsTrue(resetStatisticResult);
            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!5A\r");
            Assert.AreEqual(1, this._provider.SerialPortMock.ReadValues.Count);
            Assert.IsTrue(this._provider.SerialPortMock.ReadValues.Any(s => s.StartsWith("!5A OK\n\r")));
        }

        [TestMethod]
        public void CheckRisetime_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();
            
            var checkRisetimeResult = casySerialPortDriver.CheckRisetime(this._progressMock.Object);

            Assert.AreEqual("0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000", checkRisetimeResult);
            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!76\r");
            Assert.AreEqual(1, this._provider.SerialPortMock.ReadValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.ReadValues, "!76 OK\n\r\"76,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000\n\r");
        }

        [TestMethod]
        public void CheckTightness_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();
            
            var checkTightnessResult = casySerialPortDriver.CheckTightness(this._progressMock.Object);

            Assert.AreEqual("0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000", checkTightnessResult);
            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!75\r");
            Assert.AreEqual(1, this._provider.SerialPortMock.ReadValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.ReadValues, "!75 OK\n\r\"75,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000,0000\n\r");
        }

        [TestMethod]
        public void SendInfo_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();
            
            var sendInfoResult = casySerialPortDriver.SendInfo(this._progressMock.Object);

            Assert.IsTrue(sendInfoResult);
            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "INFO ON\r");
            Assert.AreEqual(1, this._provider.SerialPortMock.ReadValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.ReadValues, "INFO ON OK\n\r");
        }

        [TestMethod]
        public void SendSwitchToTTC_Test()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();
            casySerialPortDriver.Prepare(this._progressMock.Object);
            _provider.ConnectedAwaiter.WaitOne(60000);
            Assert.IsTrue(casySerialPortDriver.IsConnected);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();
            
            var sendSwitchToTTCResult = casySerialPortDriver.SendSwitchToTTC(this._progressMock.Object);

            Assert.IsTrue(sendSwitchToTTCResult);
            Assert.AreEqual(1, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!47#0\r");
            Assert.AreEqual(1, this._provider.SerialPortMock.ReadValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.ReadValues, "!47#0 OK\n\r");
        }

        private CasySerialPortDriver GetCasySerialPortDriver()
        {
            return _provider.CasySerialPortDriver;
            //var casySerialPortDriver = new CasySerialPortDriver(
            //    Casy.Test.Mock.Mocks.LoggerMock.Object,
            //    Casy.Test.Mock.Mocks.ConfigServiceMock.Object,
            //    Casy.Test.Mock.Mocks.LocalizationServiceMock.Object,
            //    this._provider.SerialPortMock.Object
            //    );

            //casySerialPortDriver.OnImportsSatisfied();

            //casySerialPortDriver.OnIsConnectedChangedEvent += (s, e) =>
            //{
            //    if (casySerialPortDriver.IsConnected)
            //    {
            //        this._provider.ConnectedAwaiter.Set();
            //    }
            //};

            //return casySerialPortDriver;
        }
    }
}
