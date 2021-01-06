using OLS.Casy.Core;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Config.Api;
using OLS.Casy.IO.Api;
using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace OLS.Casy.IO.BT
{
    //[PartCreationPolicy(CreationPolicy.Shared)]
    //[Export(typeof(IService))]
    //[Export(typeof(IBluetoothService))]
    public class BluetoothService : /*AbstractService,*/ IBluetoothService, IDisposable
    {
        Guid SERVICE_UUID = Guid.Parse("8fc620e0-f82e-4f2a-88ae-3ee0b5f2cd18");
        Guid CHARACTERISTIC_UUID = Guid.Parse("231ffa28-2b65-4929-9162-e383bc7ed38a");

        private GattServiceProvider _serviceProvider;
        private GattLocalCharacteristic _readCharacteristic;

        //[ImportingConstructor]
        public BluetoothService()//(IConfigService configService)
            //: base(configService)
        {
            //var server = CrossBleAdapter.Current.CreateGattServer();
        }

        public async Task InitializeServiceAsync()
        {
            try
            {
                GattServiceProviderResult result = await GattServiceProvider.CreateAsync(SERVICE_UUID);

                if(result.Error == BluetoothError.Success)
                {
                    this._serviceProvider = result.ServiceProvider;

                    GattLocalCharacteristicParameters readParameters = new GattLocalCharacteristicParameters()
                    {
                        CharacteristicProperties = GattCharacteristicProperties.Read,
                        ReadProtectionLevel = GattProtectionLevel.Plain
                    };
                    GattLocalCharacteristicResult characteristicResult = await this._serviceProvider.Service.CreateCharacteristicAsync(CHARACTERISTIC_UUID, readParameters);
                    if (characteristicResult.Error != BluetoothError.Success)
                    {
                        // An error occurred.
                        return;
                    }
                    _readCharacteristic = characteristicResult.Characteristic;
                    _readCharacteristic.ReadRequested += ReadCharacteristic_ReadRequested;

                }

                GattServiceProviderAdvertisingParameters advParameters = new GattServiceProviderAdvertisingParameters
                {
                    IsDiscoverable = true,
                    IsConnectable = true
                };
                this._serviceProvider.StartAdvertising(advParameters);
            }
            catch (Exception e)
            {
                
            }
        }

        private async void ReadCharacteristic_ReadRequested(GattLocalCharacteristic sender, GattReadRequestedEventArgs args)
        {
            var deferral = args.GetDeferral();

            // Our familiar friend - DataWriter.
            var writer = new DataWriter();
            writer.WriteString("Hallo Welt");
            // populate writer w/ some data. 
            // ... 

            var request = await args.GetRequestAsync();
            request.RespondWithValue(writer.DetachBuffer());

            deferral.Complete();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls
        

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this._serviceProvider.StopAdvertising();
                }

                disposedValue = true;
            }
        }

        ~BluetoothService()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
