using Moq;
using OLS.Casy.Com;
using OLS.Casy.Com.Api;
using OLS.Casy.Com.TTSwitch;
using OLS.Casy.Test.Mock;
using System;
using System.Threading;

namespace OLS.Casy.Com.Test
{
    public class MockedCasySerialPortDriverProvider
    {
        public MockedCasySerialPortDriverProvider(SerialPortMock serialPortMock)
        {
            var configServiceMock = Casy.Test.Mock.Mocks.ConfigServiceMock;
            configServiceMock.Setup(mock => mock.InitializeByConfiguration<CasySerialPortDriver>(It.IsAny<CasySerialPortDriver>()));

            this.ConnectedAwaiter = new ManualResetEvent(false);
            this.SerialPortMock = serialPortMock;

            this.CasySerialPortDriver = new CasySerialPortDriver(
                Casy.Test.Mock.Mocks.LoggerMock.Object,
                configServiceMock.Object,
                Casy.Test.Mock.Mocks.LocalizationServiceMock.Object,
                this.SerialPortMock.Object
                );

            this.CasySerialPortDriver.OnImportsSatisfied();

            this.CasySerialPortDriver.OnIsConnectedChangedEvent += (s, e) =>
            {
                if (this.CasySerialPortDriver.IsConnected)
                {
                    this.ConnectedAwaiter.Set();
                }
            };
        }

        public void Prepare(Mock<IProgress<string>> progressMock)
        {
            CasySerialPortDriver.Prepare(progressMock.Object);
            ConnectedAwaiter.WaitOne(60000);
        }

        public CasySerialPortDriver CasySerialPortDriver { get; private set; }
        public SerialPortMock SerialPortMock { get; private set; }
        public ManualResetEvent ConnectedAwaiter { get; private set; }
    }

    public class MockedTTSwitchServiceProvider
    {
        public MockedTTSwitchServiceProvider(MockedCasySerialPortDriverProvider mockedCasySerialPortDriverProvider)
        {
            this.TTSwitchService = new TTSwitchService(mockedCasySerialPortDriverProvider.CasySerialPortDriver);
        }

        public ITTSwitchService TTSwitchService { get; private set; }
    }
}
