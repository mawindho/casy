using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using OLS.Casy.Com.TTSwitch;
using System;

namespace OLS.Casy.Com.Test
{
    [TestClass]
    public class TTSwitchServiceTest
    {
        private Mock<IProgress<string>> _progressMock;

        private MockedCasySerialPortDriverProvider _provider;

        [TestInitialize]
        public void Initialize()
        {
            this._progressMock = new Mock<IProgress<string>>();
            this._progressMock.Setup(progress => progress.Report(It.IsAny<string>()));

            _provider = new MockedCasySerialPortDriverProvider(Casy.Test.Mock.Mocks.SerialPortMock);
        }

        [TestCleanup]
        public void CleanUp()
        {
            this._provider = null;
            this._progressMock = null;
        }

        [TestMethod]
        public void SwitchToTTC_PassValidData()
        {
            var casySerialPortDriver = GetCasySerialPortDriver();

            TTSwitchService ttSwitchService = new TTSwitchService(casySerialPortDriver);

            var result = ttSwitchService.SwitchToTTC();

            Assert.AreEqual(4, this._provider.SerialPortMock.WrittenValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "INFO ON\r"); // SendInfo
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!51\r"); // GetDateTime
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "MASTERPIN#45105\r"); // VerifyMasterPin
            CollectionAssert.Contains(this._provider.SerialPortMock.WrittenValues, "!47#0\r"); // VerifyMasterPin

            Assert.AreEqual(4, this._provider.SerialPortMock.ReadValues.Count);
            CollectionAssert.Contains(this._provider.SerialPortMock.ReadValues, "INFO ON OK\n\r"); // SendInfo
            CollectionAssert.Contains(this._provider.SerialPortMock.ReadValues, "!51 OK\n\r\"51,201804161448231234"); //GetDateTime
            CollectionAssert.Contains(this._provider.SerialPortMock.ReadValues, "MASTERPIN#45105 OK\n\r"); // VerifyMasterPin
            CollectionAssert.Contains(this._provider.SerialPortMock.ReadValues, "!47#0 OK\n\r");

            Assert.IsTrue(result);
        }

        private CasySerialPortDriver GetCasySerialPortDriver()
        {
            this._provider.Prepare(this._progressMock);

            this._provider.SerialPortMock.WrittenValues.Clear();
            this._provider.SerialPortMock.ReadValues.Clear();
            return _provider.CasySerialPortDriver;
        }
    }
}
