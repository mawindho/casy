using OLS.Casy.Core;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Config.Api;
using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Management;
using System.Threading;
using OLS.Casy.IO.Api;

namespace OLS.Casy.IO.UsbDetection
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IService))]
    [Export(typeof(IUsbDetectionService))]
    public class UsbDetectionService : AbstractService, IUsbDetectionService, IDisposable
    {
        private CancellationTokenSource _tokenSource;
        private readonly IUpdateService _updateService;

        [ImportingConstructor]
        public UsbDetectionService(IConfigService configService, IUpdateService updateService)
            : base(configService)
        {
            _updateService = updateService;
        }

        public override void Prepare(IProgress<string> progress)
        {
            base.Prepare(progress);
            _tokenSource = new CancellationTokenSource();
            var token = _tokenSource.Token;

            Task.Factory.StartNew(StartDetection, token);
        }

        public override void Deinitialize(IProgress<string> progress)
        {
            base.Deinitialize(progress);
        }

        private ManagementScope _scope;

        public event EventHandler<UsbStickDetectedEventArgs> UsbStickDetectedEvent;

        private void RaiseUsbStickDetectedEvent(string usbPath)
        {
            if (UsbStickDetectedEvent != null)
            {
                UsbStickDetectedEvent.Invoke(this, new UsbStickDetectedEventArgs(usbPath));
            }
        }

        private void StartDetection()
        {
            ConnectionOptions options = new ConnectionOptions();
            options.Impersonation = ImpersonationLevel.Impersonate;
            options.Authentication = AuthenticationLevel.Default;
            options.EnablePrivileges = true;

            _scope = new ManagementScope();
            _scope.Path = new ManagementPath(@"\\" + Environment.MachineName + @"\root\cimv2");
            _scope.Options = options;
            _scope.Connect();

            //WqlEventQuery insertQuery = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'");
            WqlEventQuery query = new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2");

            ManagementEventWatcher insertWatcher = new ManagementEventWatcher(_scope, query);
            insertWatcher.EventArrived += new EventArrivedEventHandler(DeviceInsertedEvent);
            insertWatcher.Start();

            WqlEventQuery removeQuery = new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'");
            ManagementEventWatcher removeWatcher = new ManagementEventWatcher(_scope, removeQuery);
            removeWatcher.EventArrived += new EventArrivedEventHandler(DeviceRemovedEvent);
            removeWatcher.Start();
        }

        private void DeviceRemovedEvent(object sender, EventArrivedEventArgs e)
        {
            ManagementBaseObject instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
            foreach (var property in instance.Properties)
            {
                Console.WriteLine(property.Name + " = " + property.Value);
            }
        }

        private void DeviceInsertedEvent(object sender, EventArrivedEventArgs e)
        {
            string driveName = e.NewEvent.Properties["DriveName"].Value.ToString();
            RaiseUsbStickDetectedEvent(driveName);

            _updateService.OnUsbStickDetected(driveName);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if(_tokenSource != null)
                    { 
                        this._tokenSource.Dispose();
                    }
                }

                disposedValue = true;
            }
        }

        ~UsbDetectionService() {
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
