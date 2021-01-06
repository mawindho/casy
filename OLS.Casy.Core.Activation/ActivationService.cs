using Newtonsoft.Json;
using OLS.Casy.Controller.Api;
using OLS.Casy.Core.Activation.Dialogs;
using OLS.Casy.Core.Activation.Model;
using OLS.Casy.Core.Activation.ViewModels;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.IO.Api;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OLS.Casy.Core.Activation
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IActivationService))]
    public class ActivationService : IActivationService
    {
        private readonly IFileSystemStorageService _fileSystemStorageService;
        private readonly IEncryptionProvider _encryptionProvider;
        private readonly IDatabaseStorageService _databaseStorageService;
        private readonly IEnvironmentService _environmentService;
        private readonly ICasyController _casyController;
        private readonly IUpdateService _updateService;
        private readonly IMeasureCounter _measureCounter;
        private readonly ICompositionFactory _compositionFactory;

        private string _cpuId;

        [ImportingConstructor]
        public ActivationService(IFileSystemStorageService fileSystemStorageService,
            IDatabaseStorageService databaseStorageService,
            IEncryptionProvider encryptionProvider,
            IEnvironmentService environmentService,
            ICasyController casyController,
            IUpdateService updateService,
            ICompositionFactory compositionFactory,
            [Import(AllowDefault = true)] IMeasureCounter measureCounter)
        {
            _fileSystemStorageService = fileSystemStorageService;
            _databaseStorageService = databaseStorageService;
            _encryptionProvider = encryptionProvider;
            _environmentService = environmentService;
            _casyController = casyController;
            _updateService = updateService;
            _measureCounter = measureCounter;
            _compositionFactory = compositionFactory;
        }

        public async Task<bool> CheckActivation(Action<object> showMessageDialogDelegate, Action<object> showCustomDialogDelegate, IProgress<string> splashProgress = null)
        {
            _cpuId = _environmentService.GetUniqueId();
            _environmentService.SetEnvironmentInfo("CpuId", _cpuId);

            if (!File.Exists("casy.lic"))
            {        
                if (!await RequestActivationKey(showMessageDialogDelegate, showCustomDialogDelegate, splashProgress))
                {
                    return false;
                }
            }

            var licBytes = await _fileSystemStorageService.ReadFileAsync("casy.lic");
            licBytes = _encryptionProvider.Decrypt(licBytes, _cpuId);
            License license;
            string serialNumber;

            using (var memoryStream = new MemoryStream(licBytes))
            {
                var formatter = new BinaryFormatter();
                 license = formatter.Deserialize(memoryStream) as License;

                if(license != null && license.ReloadCalibration)
                {
                    splashProgress?.Report("Loading calibration ...");

                    serialNumber = _casyController.GetSerialNumber();
                    if(string.IsNullOrEmpty(serialNumber))
                    {
                        serialNumber = license.SerialNumber;
                    }

                    if(!string.IsNullOrEmpty(serialNumber) && await DownloadCalibrationFiles(serialNumber, showMessageDialogDelegate))
                    {
                        license.ReloadCalibration = false;
                        await WriteLicense(license);
                    }
                    //Comment this section for calib buero version
                    /*try
                    {
                        using (var handler = new System.Net.Http.WebRequestHandler())
                        {
                            handler.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                            using (HttpClient httpClient = new HttpClient(handler))
                            {
                                //httpClient.BaseAddress = new Uri("http://localhost:51893/");
                                //httpClient.BaseAddress = new Uri("https://185.55.118.30:51894/");
                                httpClient.BaseAddress = new Uri("https://185.55.118.30:11372/");
                                httpClient.Timeout = TimeSpan.FromMinutes(1);
                                httpClient.DefaultRequestHeaders.Accept.Clear();
                                httpClient.DefaultRequestHeaders.Accept.Add(
                                    new MediaTypeWithQualityHeaderValue("application/json"));

                                var activationModel = new ActivationModel()
                                {
                                    ActivationKey = license.ActivationKey,
                                    SerialNumber = serialNumber,
                                    CpuId = this._environmentService.GetUniqueId(),
                                    ComputerName = System.Environment.MachineName
                                };
                                var content = new StringContent(JsonConvert.SerializeObject(activationModel), Encoding.UTF8, "application/json");
                                var response = await httpClient.PostAsync($"api/activation/update", content);
                                //HttpResponseMessage response = await this._client.PostAsJsonAsync("api/user", user);
                                //response.EnsureSuccessStatusCode();

                                var responseString = await response.Content.ReadAsStringAsync();
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // ignored
                    }*/
                }

                if (!_databaseStorageService.GetSettings().TryGetValue("LastActivationKey", out var lastActivationKey))
                {
                    await ShowPossibleActivationViolation(showMessageDialogDelegate);
                    return false;
                }

                if(lastActivationKey.Value != license.ActivationKey)
                {
                    await ShowPossibleActivationViolation(showMessageDialogDelegate);
                    return false;
                }

                _environmentService.SetEnvironmentInfo("DateTimeFormat", !_databaseStorageService.GetSettings().TryGetValue("DateTimeFormat", out var dateTimeFormatSetting) ? "System" : dateTimeFormatSetting.Value);
            }

            serialNumber = _casyController.GetSerialNumber();
            if (string.IsNullOrEmpty(serialNumber))
            {
                serialNumber = license.SerialNumber;
            }

            //Comment this section for calib buero version
            if (CheckForInternetConnection())
            {
                splashProgress?.Report("Update activation status ...");
                try
                {
                    using (var handler = new WebRequestHandler())
                    //using(var handler = new HttpClientHandler())
                    {
                        handler.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                        //handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                        using (var httpClient = new HttpClient(handler))
                        {
                            //httpClient.BaseAddress = new Uri("http://localhost:51893/");
                            //httpClient.BaseAddress = new Uri("https://185.55.118.30:51894/");
                            httpClient.BaseAddress = new Uri("https://185.55.118.30:11372/");
                            httpClient.Timeout = TimeSpan.FromMinutes(1);
                            httpClient.DefaultRequestHeaders.Accept.Clear();
                            httpClient.DefaultRequestHeaders.Accept.Add(
                                new MediaTypeWithQualityHeaderValue("application/json"));

                            var activationModel = new ActivationModel()
                            {
                                ActivationKey = license.ActivationKey,
                                SerialNumber = serialNumber,
                                CpuId = _environmentService.GetUniqueId(),
                                ComputerName = Environment.MachineName
                            };
                            var content = new StringContent(JsonConvert.SerializeObject(activationModel), Encoding.UTF8,
                                "application/json");
                            var response = await httpClient.PostAsync($"api/activation/update", content);
                            var responseString = await response.Content.ReadAsStringAsync();

                            activationModel = JsonConvert.DeserializeObject<ActivationModel>(responseString);

                            if (string.IsNullOrEmpty(activationModel.ValidationError))
                            {
                                license.ValidTo = activationModel.ValidTo;
                                license.LicenseType = activationModel.ProductType;
                                license.AddOns = activationModel.AddOns.ToArray();
                                license.SerialNumber = activationModel.SerialNumber;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            _environmentService.SetEnvironmentInfo("License", license);
            _databaseStorageService.SaveSetting("SerialNumber", license.SerialNumber);

            foreach (var addOn in license.AddOns)
            {
                _environmentService.SetEnvironmentInfo($"{addOn}Enabled", true);
            }

            try
            {
                if (license.LicenseType == "Counter")
                {
                    splashProgress?.Report("Checking counts ...");

                    using (var handler = new WebRequestHandler())
                    //using (var handler = new HttpClientHandler())
                    {
                        handler.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                        //handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                        using (var httpClient = new HttpClient(handler))
                        {
                            //httpClient.BaseAddress = new Uri("http://localhost:51893/");
                            //httpClient.BaseAddress = new Uri("https://185.55.118.30:51894/");
                            httpClient.BaseAddress = new Uri("https://185.55.118.30:11372/");
                            httpClient.Timeout = TimeSpan.FromMinutes(10);
                            httpClient.DefaultRequestHeaders.Accept.Clear();
                            httpClient.DefaultRequestHeaders.Accept.Add(
                                new MediaTypeWithQualityHeaderValue("application/json"));
                            var availableCounts = new AvailableCounts
                            {
                                ActivationKey = license.ActivationKey
                            };
                            var content = new StringContent(JsonConvert.SerializeObject(availableCounts), Encoding.UTF8, "application/json");
                            var response = await httpClient.PostAsync($"api/activation/counts", content);
                            response.EnsureSuccessStatusCode();

                            var responseString = await response.Content.ReadAsStringAsync();

                            availableCounts = JsonConvert.DeserializeObject<AvailableCounts>(responseString);

                            if (availableCounts.Counts > 0)
                            {
                                _measureCounter.DecreaseCounts(availableCounts.Counts * -1);
                            }
                        }
                    }
                }

                if (license.AddOns != null && license.AddOns.Contains("trial"))
                {
                    if(license.ValidTo < DateTime.UtcNow)
                    {
                        await Task.Factory.StartNew(() =>
                        {
                            var awaiter = new ManualResetEvent(false);

                            var messageBoxWrapper = new ShowMessageBoxDialogWrapper
                            {
                                Awaiter = awaiter,
                                Title = "Trial license expired",
                                Message = "Your trial license has been expired."
                            };

                            showMessageDialogDelegate.Invoke(messageBoxWrapper);

                            if (awaiter.WaitOne())
                            {
                            }
                        });
                        return false;
                    }

                    if (license.ValidTo < DateTime.UtcNow.AddDays(3))
                    {
                        await Task.Factory.StartNew(() =>
                        {
                            var awaiter = new ManualResetEvent(false);

                            var messageBoxWrapper = new ShowMessageBoxDialogWrapper
                            {
                                Awaiter = awaiter,
                                Title = "Trial license will expire soon",
                                Message =
                                    $"Your trial license will expire on: {license.ValidTo.ToLocalTime().ToString(CultureInfo.CurrentCulture)}"
                            };

                            showMessageDialogDelegate.Invoke(messageBoxWrapper);

                            if (awaiter.WaitOne())
                            {
                            }
                        });
                    }
                }

            }
            catch (Exception)
            {
                // ignored
            }

            return true;
        }

        private static bool CheckForInternetConnection()
        {
            try
            {
                using (var client = new WebClient())
                using (client.OpenRead("https://www.google.com/"))
                {
                    return true;
                }
            }
            catch (Exception)
            {
                // ignored
            }

            try { 
                var myPing = new Ping();
                const string host = "google.com";
                var buffer = new byte[32];
                const int timeout = 1000;
                var pingOptions = new PingOptions();
                var reply = myPing.Send(host, timeout, buffer, pingOptions);
                return reply != null && (reply.Status == IPStatus.Success);
            }
            catch (Exception)
            {
                // ignored
            }

            return false;
        }

        public async Task WriteLicense(License license)
        {
            using (var memoryStream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(memoryStream, license);

                memoryStream.Seek(0, SeekOrigin.Begin);

                var bytes = new byte[memoryStream.Length];
                memoryStream.Read(bytes, 0, (int)memoryStream.Length);

                bytes = _encryptionProvider.Encrypt(bytes, this._cpuId);

                await _fileSystemStorageService.CreateFileAsync("casy.lic", bytes);
            }
        }

        private async Task<bool> RequestActivationKey(Action<object> showMessageDialogDelegate, Action<object> showCustomDialogDelegate, IProgress<string> splashProgress)
        {
            var result = await Task.Factory.StartNew(async () =>
            {
                //Check prerequisites
                //1. Internet Connection
                var isOnline = EnvironmentService.CheckForInternetConnection();
                //2. Write permission on folder
                var everOneHasWriteAccess = EnvironmentService.HasEveryOneWritePermissions(AppDomain.CurrentDomain.BaseDirectory);
                //3. CASY connected
                var isCasyConnected = _casyController.IsConnected || _casyController.ForceCheckIsConnected();
                //4.Serial-numer
                string serialNumber = null;
                if(isCasyConnected)
                {
                    serialNumber = _casyController.GetSerialNumber();
                }

                //Uncomment this section for calib buero version
                //Only for Calibration Buero Version
                /*
                var license = new License()
                {
                    ActivationKey = "Calibration-Buero",
                    LicenseType = "Full",
                    ValidFrom = DateTime.Now,
                    ValidTo = DateTime.MaxValue,
                    AddOns = new [] { "localAuth", "cfr", "control" },
                    ReloadCalibration = false,
                    SerialNumber = ""
                };

                _environmentService.SetEnvironmentInfo("License", license);
                await WriteLicense(license);

                this._environmentService.SetEnvironmentInfo("License", license);
                _databaseStorageService.SaveSetting("LastActivationKey", "Calibration-Buero");

                return true;*/
                
                var awaiter = new ManualResetEvent(false);
                var viewModelExport = _compositionFactory.GetExport<ActivationKeyDialogViewModel>();
                var viewModel = viewModelExport.Value;

                viewModel.EveryOneHasWriteAccess = everOneHasWriteAccess;
                viewModel.IsInternetConnected = isOnline;
                viewModel.IsCasyConnected = isCasyConnected;
                viewModel.SerialNumber = serialNumber;

                var wrapper = new ShowCustomDialogWrapper
                {
                    Awaiter = awaiter,
                    DataContext = viewModel,
                    DialogType = typeof(ActivationKeyDialog)
                };

                showCustomDialogDelegate.Invoke(wrapper);

                if (awaiter.WaitOne() && !viewModel.IsCancel)
                {
                    if (!viewModel.IsCancel && !string.IsNullOrEmpty(viewModel.ActivationKey))
                    {
                        var activationKey = viewModel.ActivationKey;

                        using (var handler = new WebRequestHandler())
                        //using (var handler = new HttpClientHandler())
                        {
                            handler.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                            //handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                            using (var httpClient = new HttpClient(handler))
                            {
                                //httpClient.BaseAddress = new Uri("http://localhost:51893/");
                                //httpClient.BaseAddress = new Uri("https://185.55.118.30:51894/");
                                httpClient.BaseAddress = new Uri("https://185.55.118.30:11372/");
                                httpClient.Timeout = TimeSpan.FromMinutes(10);
                                httpClient.DefaultRequestHeaders.Accept.Clear();
                                httpClient.DefaultRequestHeaders.Accept.Add(
                                    new MediaTypeWithQualityHeaderValue("application/json"));

                                var activationModel = new ActivationModel()
                                {
                                    ActivationKey = activationKey,
                                    SerialNumber = serialNumber ?? string.Empty,
                                    CpuId = _environmentService.GetUniqueId(),
                                    ComputerName = Environment.MachineName
                                };

                                try
                                {
                                    var content = new StringContent(JsonConvert.SerializeObject(activationModel), Encoding.UTF8, "application/json");
                                    var response = await httpClient.PostAsync("api/activation", content);
                                    response.EnsureSuccessStatusCode();

                                    var responseString = await response.Content.ReadAsStringAsync();

                                    activationModel = JsonConvert.DeserializeObject<ActivationModel>(responseString);

                                    if(!string.IsNullOrEmpty(activationModel.ValidationError))
                                    {
                                        await Task.Factory.StartNew(() =>
                                        {
                                            awaiter = new ManualResetEvent(false);

                                            var messageBoxWrapper = new ShowMessageBoxDialogWrapper
                                            {
                                                Awaiter = awaiter,
                                                Title = "Activation validation failed",
                                                Message = activationModel.ValidationError
                                            };

                                            showMessageDialogDelegate.Invoke(messageBoxWrapper);

                                            if (awaiter.WaitOne())
                                            {
                                            }
                                        });
                                        return false;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    await Task.Factory.StartNew(() =>
                                    {
                                        awaiter = new ManualResetEvent(false);

                                        var messageBoxWrapper = new ShowMessageBoxDialogWrapper
                                        {
                                            Awaiter = awaiter,
                                            Title = "Activation server not reachable",
                                            Message = "The activation server seems to be not reachable. Please check your internet connectivity and retry later.\n" + ex.Message
                                        };

                                        showMessageDialogDelegate.Invoke(messageBoxWrapper);

                                        if (awaiter.WaitOne())
                                        {
                                        }
                                    });
                                    return false;
                                }

                                if (activationModel.IsValid)
                                {
                                    serialNumber = activationModel.SerialNumber;

                                    splashProgress?.Report("Loading calibration ...");

                                    var loadingCalibrationSuccess =
                                        await DownloadCalibrationFiles(serialNumber, showMessageDialogDelegate);
                                        
                                    var license = new License
                                    {
                                        ActivationKey = activationModel.ActivationKey,
                                        LicenseType = activationModel.ProductType,
                                        ValidFrom = activationModel.ValidFrom,
                                        ValidTo = activationModel.ValidTo,
                                        AddOns = activationModel.AddOns.ToArray(),
                                        ReloadCalibration = !loadingCalibrationSuccess,
                                        SerialNumber = activationModel.SerialNumber
                                    };

                                    _environmentService.SetEnvironmentInfo("License", license);
                                    await WriteLicense(license);

                                    _environmentService.SetEnvironmentInfo("License", license);
                                    _databaseStorageService.SaveSetting("LastActivationKey", activationKey);
                                    

                                    try
                                    {
                                        splashProgress?.Report("Loading latest software version ...");
                                        await _updateService.CheckForUpdates(false, splashProgress, "0.0.0.0");
                                    }
                                    catch (Exception ex)
                                    {
                                        await Task.Factory.StartNew(() =>
                                        {
                                            awaiter = new ManualResetEvent(false);

                                            var messageBoxWrapper2 = new ShowMessageBoxDialogWrapper
                                            {
                                                Awaiter = awaiter,
                                                Title = "Unable to download latest update",
                                                Message = "Downloading the latest update failed. Please restart the software and try downloading the latest software update manually.\n"+ ex.Message
                                            };

                                            showMessageDialogDelegate.Invoke(messageBoxWrapper2);

                                            if (awaiter.WaitOne())
                                            {
                                            }
                                        });
                                        return false;
                                    }

                                    return false;
                                }

                                awaiter = new ManualResetEvent(false);

                                var messageBoxWrapper3 = new ShowMessageBoxDialogWrapper
                                {
                                    Awaiter = awaiter,
                                    Title = "Activation failed",
                                    Message = activationModel.ValidationError
                                };
                               
                                showMessageDialogDelegate.Invoke(messageBoxWrapper3);

                                if (awaiter.WaitOne())
                                {
                                }
                            }
                        }
                    }
                }

                _compositionFactory.ReleaseExport(viewModelExport);

                return false;
            });

            return result.Result;
        }

        private async Task<bool> DownloadCalibrationFiles(string serialNumber, Action<object> showMessageDialogDelegate)
        {
            if (string.IsNullOrEmpty(serialNumber)) return false;

            try
            {
                using (var handler = new WebRequestHandler())
                //using (var handler = new HttpClientHandler())
                {
                    handler.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                    //handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                    using (var httpClient = new HttpClient(handler))
                    {
                        //httpClient.BaseAddress = new Uri("http://localhost:51893/");
                        //httpClient.BaseAddress = new Uri("https://185.55.118.30:51894/");
                        httpClient.BaseAddress = new Uri("https://185.55.118.30:11372/");
                        httpClient.Timeout = TimeSpan.FromMinutes(10);
                        httpClient.DefaultRequestHeaders.Accept.Clear();
                        httpClient.DefaultRequestHeaders.Accept.Add(
                            new MediaTypeWithQualityHeaderValue("application/json"));

                        var response = await httpClient.GetAsync($@"api/activation/calib/{serialNumber}");
                        response.EnsureSuccessStatusCode();

                        var calibTempFile = Path.GetTempFileName();
                        using (var output = File.OpenWrite(calibTempFile))
                        using (var input = await response.Content.ReadAsStreamAsync())
                        {
                            input.CopyTo(output);
                        }

                        var calibTempDir = Path.Combine(Path.GetDirectoryName(calibTempFile), Path.GetFileNameWithoutExtension(calibTempFile)) + Path.DirectorySeparatorChar;
                        _updateService.ExtractZipFile(calibTempFile, string.Empty, calibTempDir);

                        var calibrationDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Calibration");

                        if (!Directory.Exists(calibrationDir))
                        {
                            Directory.CreateDirectory(calibrationDir);
                        }

                        var dirInfo = new DirectoryInfo(calibTempDir);

                        var files = dirInfo.GetFiles();

                        var processedFiles = new List<string>();

                        foreach (var file in files)
                        {
                            var origFilePath = Path.Combine(calibrationDir, file.Name);
                            if (File.Exists(origFilePath))
                            {
                                var oldFileName = $"{origFilePath}.old";
                                var oldFile = new FileInfo(origFilePath);

                                oldFile.MoveTo(oldFileName);

                                processedFiles.Add(oldFileName);
                            }

                            await _fileSystemStorageService.CopyFileAsync(file.FullName, origFilePath);
                        }

                        foreach (var fileName in processedFiles)
                        {
                            await _fileSystemStorageService.DeleteFileAsync(fileName);
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                await Task.Factory.StartNew(() =>
                {
                    var awaiter = new ManualResetEvent(false);

                    var messageBoxWrapper = new ShowMessageBoxDialogWrapper
                    {
                        Awaiter = awaiter,
                        Title = "Unable to download calibration files",
                        Message = "Downloading calibration info for your device have failed. Please contact your distribution center.\n" + ex.Message
                    };

                    showMessageDialogDelegate.Invoke(messageBoxWrapper);

                    if (awaiter.WaitOne())
                    {
                    }
                });
                return false;
            }
        }

        private static async Task ShowPossibleActivationViolation(Action<object> showMessageDialogDelegate)
        {
            await Task.Factory.StartNew(() =>
            {
                var awaiter = new ManualResetEvent(false);

                var messageBoxWrapper = new ShowMessageBoxDialogWrapper
                {
                    Awaiter = awaiter,
                    Title = "Possible activation violation",
                    Message = "A possible violation of activation mechanism has been detected! Please contact distribution center."
                };

                showMessageDialogDelegate.Invoke(messageBoxWrapper);

                if (awaiter.WaitOne())
                {
                }
            });
        }

        public bool IsModuleEnabled(string module)
        {
            return _environmentService.GetEnvironmentInfo("License") is License license && license.AddOns.Contains(module);
        }
    }
}
