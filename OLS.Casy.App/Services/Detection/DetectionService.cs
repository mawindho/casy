using OLS.Casy.App.Models;
using Polly;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace OLS.Casy.App.Services.Detection
{
    public class DetectionService : IDetectionService
    {
        private const string CommandName = "OLS_BROADCAST";
        private const string HeaderIdParam = "bdc_headerid";
        private const ulong HeaderIdValue = 0x87654321;
        private const string CommandLineEnd = "\r\n";
        private const int UdpBroadcastPort = 34569;

        private readonly UdpClient _listener;
        private System.Timers.Timer _broadcastMessageTimer;

        private TaskFactory _taskFactory;
        private CancellationTokenSource _tokenSource;

        private readonly BlockingCollection<byte[]> _datagramQueue;

        private Dictionary<string, CasyModel> _casyModels;

        public DetectionService()
        {
            var receiverEp = new IPEndPoint(IPAddress.Any, UdpBroadcastPort);
            _listener = new UdpClient();
            _listener.ExclusiveAddressUse = false;
            _listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _listener.Client.Bind(receiverEp);

            _datagramQueue = new BlockingCollection<byte[]>();

            _casyModels = new Dictionary<string, CasyModel>();
        }

        public void Initialize()
        {
            _tokenSource = new CancellationTokenSource();
            var token = _tokenSource.Token;

            _taskFactory = new TaskFactory(token);
            _taskFactory.StartNew(ProcessDatagrams, TaskCreationOptions.LongRunning);

            StartListening();

            _broadcastMessageTimer = new System.Timers.Timer { Interval = 120000, AutoReset = true };
            _broadcastMessageTimer.Elapsed += OnBroadcastTimerElapsed;
            _broadcastMessageTimer.Start();

            OnBroadcastTimerElapsed(null, null);
        }

        public IEnumerable<CasyModel> CasyModels => _casyModels.Values.ToList();

        private bool TryAddCasy(CasyModel casyModel)
        {
            if (!_casyModels.ContainsKey(casyModel.SerialNumber))
            {
                _casyModels.Add(casyModel.SerialNumber, casyModel);
                return true;
            }
            return false;
        }

        private void StartListening()
        {
            _listener.BeginReceive(OnReceive, new object());
        }

        private void OnReceive(IAsyncResult ar)
        {
            var ip = new IPEndPoint(IPAddress.Any, UdpBroadcastPort);
            var bytes = _listener.EndReceive(ar, ref ip);
            _datagramQueue.Add(bytes);

            StartListening();
        }

        private async void ProcessDatagrams()
        {
            foreach (var dgram in _datagramQueue.GetConsumingEnumerable(_taskFactory.CancellationToken))
            {
                var message = Encoding.ASCII.GetString(dgram);

                Console.WriteLine("Received: {0}", message);

                var responseHeader = CreateBroadcastMessage().Replace(CommandLineEnd, string.Empty);
                if (!message.StartsWith(responseHeader))
                {
                    continue;
                }

                message = message.Replace(responseHeader, string.Empty);
                var split = message.Split(new[] { " bdc_" }, StringSplitOptions.RemoveEmptyEntries);

                if (split.Length <= 1)
                {
                    continue;
                }

                var keyValuePairs = split.Select(item => item.Split(new[] { ' ' }, 2))
                    .ToDictionary(item => item[0], item => item[1]);

                var ipAddress = keyValuePairs["ipaddress"].Replace("\"", string.Empty);
                int ipport = -1;
                int.TryParse(keyValuePairs["ipport"], out ipport);
                var macAddress = keyValuePairs["macaddress"].Replace("\"", string.Empty);
                var systemId = keyValuePairs["systemid"].Replace("\"", string.Empty);
                var deviceName = keyValuePairs["modulename"].Replace("\"", string.Empty);
                var deviceDescription = keyValuePairs["moduledesc"].Replace("\"", string.Empty);
                var deviceNumber = keyValuePairs["modulenumber"].Replace("\"", string.Empty);

                if (deviceName == "CASY")
                {
                    Console.WriteLine("CASY detected: " + systemId);

                    if (!CasyModels.Any(x => x.SerialNumber == systemId))
                    {
                        var result = string.Empty;
                        var ipAddresses = ipAddress.Split(';');
                        foreach (var ip in ipAddresses)
                        {
                            using (var httpClient = new HttpClient())
                            {
                                var byteArray = Encoding.ASCII.GetBytes("casy:c4sy");
                                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                                var url = $"http://{ip}:8536/";

                                try
                                {
                                    var response = await Policy.Handle<HttpRequestException>().OrResult<HttpResponseMessage>(msg => !msg.IsSuccessStatusCode)
                                        .WaitAndRetryAsync(2, i => TimeSpan.FromSeconds(0.5), (res, timeSpan, retryCount, context) =>
                                        {
                                        })
                                        .ExecuteAsync(() => httpClient.GetAsync($"{url}ping"));

                                    if (response.IsSuccessStatusCode)
                                    {
                                        result = ip;
                                        break;
                                    }
                                }
                                catch
                                { }
                            }
                        }

                        if (!string.IsNullOrEmpty(result))
                        {
                            TryAddCasy(new Models.CasyModel()
                            {
                                SerialNumber = systemId,
                                IpAddress = result,
                                DeviceName = deviceName
                            });
                        }
                    }
                }
            }
        }

        private static void Send(string message)
        {
            var client = new UdpClient();
            client.ExclusiveAddressUse = false;
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            var endpoint = new IPEndPoint(IPAddress.Broadcast, UdpBroadcastPort);
            var bytes = Encoding.ASCII.GetBytes(message);
            client.Send(bytes, bytes.Length, endpoint);
            client.Close();
        }

        private void OnBroadcastTimerElapsed(object sender, ElapsedEventArgs e)
        {
            Send(CreateBroadcastMessage());
        }

        private static string CreateBroadcastMessage()
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
    }
}
