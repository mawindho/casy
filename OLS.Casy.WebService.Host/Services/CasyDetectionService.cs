using Microsoft.Extensions.Hosting;
using OLS.Casy.IO.SQLite.Standard;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace OLS.Casy.WebService.Host.Services
{
    public class CasyDetectionService : IHostedService, IDisposable
    {
        private const string CommandName = "OLS_BROADCAST";
        private const string HeaderIdParam = "bdc_headerid";
        private const ulong HeaderIdValue = 0x87654321;
        private const string CounterParam = "bdc_counter";
        private const string IpStateParam = "bdc_ipstate";
        private const string IpAddressParam = "bdc_ipaddress";
        private const string IpPortParam = "bdc_ipport";
        private const string MacAddressParam = "bdc_macaddress";
        private const string SystemIdParam = "bdc_systemid";
        private const string ModuleNameParam = "bdc_modulename";
        private const string ModuleDescParam = "bdc_moduledesc";
        private const string ModuleNumberParam = "bdc_modulenumber";
        private const string ModuleModeParam = "bdc_modulemode"; //Checken, ob hier vielleicht auch offline geht (in CERO SW)
        private const string HamSmartPortParam = "bdc_hamsmartport";
        private const string CommandLineEnd = "\r\n";

        private const int UdpBroadcastPort = 34569;

        private UdpClient _udpListener;
        //private System.Timers.Timer _broadcastMessageTimer;

        private TaskFactory _taskFactory;
        private CancellationTokenSource _cts;

        private BlockingCollection<byte[]> _datagramQueue;
        private readonly CasyContext _casyContext;

        public CasyDetectionService(CasyContext casyContext)
        {
            _casyContext = casyContext;

            var receiverEp = new IPEndPoint(IPAddress.Any, UdpBroadcastPort);
            _udpListener = new UdpClient();
            _udpListener.ExclusiveAddressUse = false;
            //_udpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            //_udpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, false);
            _udpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpListener.Client.Bind(receiverEp);

            _datagramQueue = new BlockingCollection<byte[]>();
        }

        private void StartListening()
        {
            _udpListener.BeginReceive(OnUdpBroadcastReceive, new object());
        }

        private void OnUdpBroadcastReceive(IAsyncResult ar)
        {
            var ip = new IPEndPoint(IPAddress.Any, UdpBroadcastPort);
            var bytes = _udpListener.EndReceive(ar, ref ip);
            _datagramQueue.Add(bytes);

            StartListening();
        }

        private void ProcessDatagrams()
        {
            try
            {
                foreach (var dgram in _datagramQueue.GetConsumingEnumerable(_cts.Token))
                {
                    if (_cts.IsCancellationRequested)
                    {
                        return;
                    }

                    var message = Encoding.ASCII.GetString(dgram);

                    var responseHeader = CreateBaseBroadcastMessage();
                    if (!message.Equals(responseHeader))
                    {
                        continue;
                    }

                    BroadcastCasyOnlineMessage();
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Cancel");
                return;
            }
        }

        private static string CreateBaseBroadcastMessage()
        {
            var datagram = new StringBuilder();
            datagram.Append(CommandName);
            datagram.Append(" ");
            datagram.Append(HeaderIdParam);
            datagram.Append(" ");
            datagram.Append(HeaderIdValue.ToString());
            datagram.Append(CommandLineEnd);

            return datagram.ToString();
        }

        private void BroadcastCasyOnlineMessage()
        {
            var message = CreateCasyOnlineBroadcastMessage();
            if (string.IsNullOrEmpty(message)) return;

            var udpClient = new UdpClient();
            udpClient.ExclusiveAddressUse = false;
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            var endpoint = new IPEndPoint(IPAddress.Broadcast, UdpBroadcastPort);
            
            var bytes = Encoding.ASCII.GetBytes(message);
            udpClient.Send(bytes, bytes.Length, endpoint);
            udpClient.Close();
        }

        private string CreateCasyOnlineBroadcastMessage()
        {
            var serialSetting = _casyContext.Settings.FirstOrDefault(x => x.Key == "SerialNumber");
            if (serialSetting == null) return string.Empty;

            var ipAddresses = GetLocalIpAddress();

            var datagram = new StringBuilder();
            datagram.Append(CommandName);
            datagram.Append(" ");
            datagram.Append(HeaderIdParam);
            datagram.Append(" ");
            datagram.Append(HeaderIdValue.ToString());
            datagram.Append(" ");
            datagram.Append(CounterParam);
            datagram.Append(" ");
            datagram.Append("0");
            datagram.Append(" ");
            datagram.Append(IpStateParam);
            datagram.Append(" ");
            datagram.Append("2");
            datagram.Append(" ");
            datagram.Append(IpAddressParam);
            datagram.Append(" ");
            datagram.Append($"\"{string.Join(";", ipAddresses)}\"");
            //datagram.Append($"\"127.0.0.1\"");
            datagram.Append(" ");
            datagram.Append(IpPortParam);
            datagram.Append(" ");
            datagram.Append("");
            datagram.Append(" ");
            datagram.Append(MacAddressParam);
            datagram.Append(" ");
            datagram.Append("");
            datagram.Append(" ");
            datagram.Append(SystemIdParam);
            datagram.Append(" ");

            var serialNumber = serialSetting.Value;

            datagram.Append($"\"{serialNumber}\"");
            datagram.Append(" ");
            datagram.Append(ModuleNameParam);
            datagram.Append(" ");
            datagram.Append("\"CASY\"");
            datagram.Append(" ");
            datagram.Append(ModuleDescParam);
            datagram.Append(" ");
            datagram.Append($"\"\"");
            datagram.Append(" ");
            datagram.Append(ModuleNumberParam);
            datagram.Append(" ");
            datagram.Append($"\"{serialNumber}\"");
            datagram.Append(" ");
            datagram.Append(ModuleModeParam);
            datagram.Append(" ");
            datagram.Append("2");
            datagram.Append(" ");
            datagram.Append(HamSmartPortParam);
            datagram.Append(" ");
            datagram.Append("0");
            datagram.Append(" ");
            datagram.Append(CommandLineEnd);

            return datagram.ToString();
        }

        private string[] GetLocalIpAddress()
        {
            List<string> ipAddresses = new List<string>();

            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface network in networkInterfaces)
            {
                if (network.OperationalStatus == OperationalStatus.Up)
                {
                    // Read the IP configuration for each network 
                    IPInterfaceProperties properties = network.GetIPProperties();

                    // Each network interface may have multiple IP addresses 
                    foreach (IPAddressInformation address in properties.UnicastAddresses)
                    {
                        // We're only interested in IPv4 addresses for now 
                        if (address.Address.AddressFamily != AddressFamily.InterNetwork)
                            continue;

                        // Ignore loopback addresses (e.g., 127.0.0.1) 
                        if (IPAddress.IsLoopback(address.Address))
                            continue;

                        //if (!address.Address.ToString().StartsWith("192"))
                        //continue;

                        ipAddresses.Add(address.Address.ToString());
                    }
                }
            }

            return ipAddresses.ToArray();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            StartListening();

            _cts = new CancellationTokenSource();
            _taskFactory = new TaskFactory(_cts.Token);
            _taskFactory.StartNew(ProcessDatagrams, TaskCreationOptions.LongRunning);

            //_logger.LogTrace($"Start listening on port {UdpBroadcastPort}");
            

            BroadcastCasyOnlineMessage();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _cts.Cancel();
                Thread.Sleep(1000);

                _udpListener.Close();
                _datagramQueue.CompleteAdding();
                //_logger.LogTrace("Stop listening");
            }
            catch (Exception)
            {
                // ignored
            }
            return Task.CompletedTask;
        }

        #region IDisposable Support
        private bool _disposedValue; // To detect redundant calls

        private void Dispose(bool disposing)
        {
            if (_disposedValue)
            {
                return;
            }

            if (disposing)
            {
                //_broadcastMessageTimer.Dispose();
            }

            _disposedValue = true;
        }

        ~CasyDetectionService()
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
