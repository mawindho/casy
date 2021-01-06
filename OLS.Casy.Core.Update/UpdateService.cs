using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using OLS.Casy.Core.Activation;
using OLS.Casy.Core.Activation.Model;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Config.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.Ui.Core.Api;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

namespace OLS.Casy.Core.Update
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    //[Export(typeof(IService))]
    [Export(typeof(IUpdateService))]
    public class UpdateService : /*AbstractService,*/ IUpdateService, IPartImportsSatisfiedNotification
    {
        private const string UpdaterExeNamespace = "OLS.Casy.Core.Update.Ui";
        private const string UpdateInfoFile = "updateInfo.xml";

        //private readonly IUsbDetectionService _usbDetectionService;
        private readonly IEnvironmentService _environmentService;
        private readonly IFileSystemStorageService _fileSystemStorageService;
        private readonly IMeasureResultManager _generalMeasureResultManager;
        private readonly IEventAggregatorProvider _eventAggregatorProvider;
        private readonly IAuthenticationService _authenticationService;
        private readonly ILocalizationService _localizationService;
        private readonly IDatabaseStorageService _databaseStorageService;

        [ImportingConstructor]
        public UpdateService(IConfigService configService, 
            //IUsbDetectionService usbDetectionService,
            IEnvironmentService environmentService,
            IFileSystemStorageService fileSystemStorageService,
            IMeasureResultManager generalMeasureResultManager,
            IAuthenticationService authenticationService,
            IEventAggregatorProvider eventAggregatorProvider,
            ILocalizationService localizationService,
            IDatabaseStorageService databaseStorageService
            )
            //: base(configService)
        {
            //_usbDetectionService = usbDetectionService;
            _environmentService = environmentService;
            _fileSystemStorageService = fileSystemStorageService;
            _generalMeasureResultManager = generalMeasureResultManager;
            _authenticationService = authenticationService;
            _eventAggregatorProvider = eventAggregatorProvider;
            _localizationService = localizationService;
            _databaseStorageService = databaseStorageService;
        }

        //public override void Prepare(IProgress<string> progress)
        //{
         //   base.Prepare(progress);
         //   _usbDetectionService.UsbStickDetectedEvent += OnUsbStickDetected;
        //}

        public void OnImportsSatisfied()
        {
            _authenticationService.UserLoggedIn += (s, e) => CheckForOnlineUpdate();
        }

        public async Task OnUsbStickDetected(string usbPath)
        {
            if (_authenticationService.LoggedInUser == null ||
                _authenticationService.LoggedInUser.UserRole.Priority <= 2) return;
            var updateInfoFileInfo = FindUpdateFile(usbPath, UpdateInfoFile);

            //var additionalUpdateFileInfo = FindUpdateFile(e.UsbPath, AdditinalUpdateInfoFile);

            if (updateInfoFileInfo == null) return;
            var updateInfoContent = await _fileSystemStorageService.ReadXmlFileAsync(updateInfoFileInfo.FullName);

            var updateVersions = CheckForUpdates(updateInfoContent, out var currentVersion);
            //if (additionalUpdateFileInfo != null)
            //{
            //    var additionalUpdateInfoContent = await this._fileSystemStorageService.ReadXmlFileAsync(additionalUpdateFileInfo.FullName);
            //    var additionalVersions = this.CheckForUpdates(additionalUpdateInfoContent, out currentVersion);
            //    updateVersions.AddRange(additionalVersions);
            //}

            var orderedUpdateInfos = updateVersions.OrderBy(info => new Version(info.Version)).ToList();
            var forceRestartItem = orderedUpdateInfos.FirstOrDefault(ui => ui.ForceRestart);

            if (forceRestartItem != null)
            {
                var index = orderedUpdateInfos.IndexOf(forceRestartItem) + 1;

                var length = orderedUpdateInfos.Count;
                for (var i = index; i < length; i++)
                {
                    orderedUpdateInfos.RemoveAt(i);
                }
            }

            if (!orderedUpdateInfos.Any()) return;
            var newVersion = orderedUpdateInfos.Select(update => update.Version).Max().ToString();

            var confirmationResult = await Task.Factory.StartNew(() =>
            {
                var awaiter = new ManualResetEvent(false);

                var messageBoxWrapper = new ShowMessageBoxDialogWrapper
                {
                    Awaiter = awaiter,
                    Title = "UpdateDialog_Title",
                    Message = "UpdateDialog_Content",
                    MessageParameter = new[] { currentVersion, newVersion }
                };

                if (orderedUpdateInfos.Any(uv => uv.ForceRestart))
                {
                    messageBoxWrapper.Message = "UpdateDialog_Content_Incremental";
                }

                _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>().Publish(messageBoxWrapper);

                var success = false;
                if (awaiter.WaitOne())
                {
                    success = messageBoxWrapper.Result;
                }

                return success;
            });


            if (!confirmationResult) return;
            var license = _environmentService.GetEnvironmentInfo("License") as License;

            var showProgressWrapper = new ShowProgressDialogWrapper
            {
                Title = "ProgressBox_UsbUpdate_Title",
                Message = "ProgressBox_UsbUpdate_Message",
                MessageParameter = new[]
                {
                    string.Format(
                        _localizationService.GetLocalizedString("ProgressBox_UsbUpdate_Message_Extracting"))
                },
                IsFinished = false
            };

            _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>().Publish(showProgressWrapper);

            var temp = Path.GetTempFileName();
            foreach (var updateVersion in orderedUpdateInfos)
            {
                updateVersion.UpdateDirectory = Path.Combine(Path.GetDirectoryName(temp), Path.GetFileNameWithoutExtension(temp)) + Path.DirectorySeparatorChar;
                ExtractZipFile(Path.Combine(usbPath, updateVersion.Version, "main.zip"), string.Empty, updateVersion.UpdateDirectory);

                var filesToDeleteList = new List<string>(updateVersion.FilesToDelete);

                foreach (var addOn in license.AddOns)
                {
                    ExtractZipFile(Path.Combine(usbPath, updateVersion.Version, $"{addOn}.zip"), string.Empty, updateVersion.UpdateDirectory);

                    switch (addOn)
                    {
                        case "adAuth":
                        case "localAuth":
                            filesToDeleteList.Add("OLS.Casy.Core.Authorization.Default.dll");
                            break;
                        case "simulator":
                            filesToDeleteList.Add("OLS.Casy.Com.dll");
                            break;
                        case "network":
                            filesToDeleteList.Add("OLS.Casy.IO.SQLite.EF.dll");
                            filesToDeleteList.Add("OLS.Casy.IO.SQLite.EF.dll.config");
                            break;
                    }
                }

                updateVersion.FilesToDelete = filesToDeleteList.ToArray();

                if (updateVersion.ForceRestart)
                {
                    break;
                }
            }

            var applicationPath = AppDomain.CurrentDomain.BaseDirectory;

            foreach (var updateInfo in orderedUpdateInfos)
            {
                var dirInfo = new DirectoryInfo(updateInfo.UpdateDirectory);
                var files = dirInfo.GetFiles();

                var updaterUiFiles = files.Where(file => Path.GetFileNameWithoutExtension(file.Name).StartsWith(UpdaterExeNamespace)).ToList();
                if (updaterUiFiles.Any())
                {
                    //updaterUiFiles.AddRange(files.Where(file =>
                        //Path.GetFileNameWithoutExtension(file.Name).StartsWith("Newtonsoft.Json")));
                    //updaterUiFiles.AddRange(files.Where(file =>
                        //Path.GetFileNameWithoutExtension(file.Name).StartsWith("log4net")));

                    foreach (var updaterUiFile in updaterUiFiles)
                    {
                        var origFilePath = Path.Combine(applicationPath, updaterUiFile.Name);
                        var tempFileName = $"{origFilePath}.temp";

                        if (File.Exists(tempFileName))
                        {
                            File.Delete(tempFileName);
                        }

                        var tempFile = new FileInfo(origFilePath);
                        tempFile.MoveTo(tempFileName);

                        //_fileSystemStorageService.GetProcessesLockingFile(tempFileName);

                        await _fileSystemStorageService.CopyFileAsync(updaterUiFile.FullName, origFilePath);
                    }
                }

                if (updateInfo.ForceRestart)
                {
                    break;
                }
            }

            await Task.Factory.StartNew(async () =>
            {
                var result = await _generalMeasureResultManager.SaveChangedMeasureResults();
                if (result != ButtonResult.Cancel)
                {
                }
            });

            _databaseStorageService.SaveSetting("ShowReleaseNodes", "1");

            showProgressWrapper.MessageParameter[0] += "\n";
            showProgressWrapper.MessageParameter[0] += string.Format(_localizationService.GetLocalizedString("ProgressBox_UsbUpdate_Message_ShutingDown"));

            Thread.Sleep(500);

            showProgressWrapper.IsFinished = true;
            _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>().Publish(showProgressWrapper);

            Thread.Sleep(500);

            var process = new Process
            {
                StartInfo =
                {
                    FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        "OLS.Casy.Core.Update.Ui.exe")
                }
            };

            var updateInfoFile = Path.GetTempFileName();
            File.WriteAllText(updateInfoFile, JsonConvert.SerializeObject(orderedUpdateInfos));

            string[] args = { "\"" + updateInfoFile + "\"", "\"" + AppDomain.CurrentDomain.BaseDirectory + "\\\"", "\"" + Process.GetCurrentProcess().Id + "\"" };

            process.StartInfo.Arguments = string.Join(" ", args);
            process.StartInfo.Verb = "runas";
            process.Start();

            Application.Current.Dispatcher.Invoke(() =>
            {
                Application.Current.Shutdown();
            });
        }

        private IEnumerable<UpdateVersion> CheckForUpdates(string updateInfoFileContent, out string currentVersionString)
        {
            var result = new List<UpdateVersion>();

            currentVersionString = _environmentService.GetEnvironmentInfo("SoftwareVersion") as string;
            var currentVersion = new Version(currentVersionString);

            var updateInfoXml = XDocument.Parse(updateInfoFileContent);

            foreach (var update in updateInfoXml.Descendants("Update"))
            {
                var versionString = update.Descendants("Version").FirstOrDefault().Value;
                var forceRestart = bool.Parse(update.Descendants("ForceRestart").FirstOrDefault().Value);

                var filesToDelete = new List<string>();
                var toDelete = update.Descendants("ToDelete").FirstOrDefault();
                if(toDelete != null)
                {
                    var files = toDelete.Descendants("File").ToList();

                    foreach(var file in files)
                    {
                        filesToDelete.Add(file.Value);
                    }
                }

                var version = new Version(versionString);

                if(version > currentVersion)
                {
                    result.Add(new UpdateVersion
                    {
                        Version = version.ToString(),
                        ForceRestart = forceRestart,
                        FilesToDelete = filesToDelete.ToArray(),
                    });
                }
            }

            return result;
        }

        private static FileInfo FindUpdateFile(string directory, string fileName)
        {
            directory = directory.EndsWith(@"\") ? directory : directory + @"\";

            var dirInfo = new DirectoryInfo(directory);

            foreach (var fileInfo in dirInfo.GetFiles())
            {
                if (fileInfo.Name == fileName)
                {
                    return fileInfo;
                }
            }

            return (from subDirInfo in dirInfo.GetDirectories() where (subDirInfo.Attributes & FileAttributes.System) != FileAttributes.System select FindUpdateFile(subDirInfo.FullName, fileName)).FirstOrDefault(fileInfo => fileInfo != null);
        }

        public void CheckForOnlineUpdate()
        {
            Task.Factory.StartNew(async () =>
            {
                if (_databaseStorageService.GetSettings().TryGetValue("ShowReleaseNodes", out var showReleaseNodes))
                {
                    if (!string.IsNullOrEmpty(showReleaseNodes.Value) && showReleaseNodes.Value == "1")
                    {
                        var license = _environmentService.GetEnvironmentInfo("License") as License;

                        var awaiter = new ManualResetEvent(false);

                        var dateTimeString = _environmentService.GetDateTimeString(license.ValidTo);

                        var messageBoxWrapper = new ShowMessageBoxDialogWrapper
                        {
                            Awaiter = awaiter,
                            Title = "UpdateService_ReleaseNodes_Title",
                            Message = "UpdateService_ReleaseNodes_Message",
                            MessageParameter = new [] { dateTimeString }
                        };

                        _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>().Publish(messageBoxWrapper);

                        if (awaiter.WaitOne())
                        {
                        }

                        _databaseStorageService.SaveSetting("ShowReleaseNodes", "0");
                    }

                    if (_authenticationService.LoggedInUser.UserRole.Priority > 2 &&
                        EnvironmentService.CheckForInternetConnection())
                    {
                        await CheckForUpdates();
                    }
                }
            });
        }

        public async Task CheckForUpdates(bool confirmationRequired = true, IProgress<string> outerProgress = null, string currentVersionParam = null)
        {
            var updateChannel = "ReleaseC";
            if (_databaseStorageService.GetSettings().TryGetValue("UpdateChannel", out var updateChannelSetting))
            {
                updateChannel = updateChannelSetting.Value;
            }

            //using (var handler = new WebRequestHandler())
            using (var handler = new HttpClientHandler())
            {
                //handler.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                using (var httpClient = new HttpClient(handler))
                {
                    //httpClient.BaseAddress = new Uri("http://localhost:51893/");
                    //httpClient.BaseAddress = new Uri("https://185.55.118.30:51894/");
                    httpClient.BaseAddress = new Uri("https://185.55.118.30:11372/");
                    httpClient.Timeout = TimeSpan.FromMinutes(10);
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));

                    var license = _environmentService.GetEnvironmentInfo("License") as License;

                    string currentVersionString;
                    if (!string.IsNullOrEmpty(currentVersionParam))
                    {
                        currentVersionString = currentVersionParam;
                    }
                    else
                    {
                        currentVersionString = _environmentService.GetEnvironmentInfo("SoftwareVersion") as string;
                    }
                    if (string.IsNullOrEmpty(currentVersionString))
                    {
                        currentVersionString = "0.0.0.0";
                    }

                    var currentVersion = new Version(currentVersionString);

                    var updateRequest = new UpdateRequest
                    {
                        ActivationKey = license.ActivationKey,
                        CurrentVersion = currentVersion,
                        Environment = updateChannel,
                        CpuId = _environmentService.GetUniqueId()
                    };

                    try
                    {
                        var content = new StringContent(JsonConvert.SerializeObject(updateRequest), Encoding.UTF8,
                            "application/json");
                        var response = await httpClient.PostAsync($"api/update", content);
                        response.EnsureSuccessStatusCode();

                        var responseString = await response.Content.ReadAsStringAsync();
                        updateRequest = JsonConvert.DeserializeObject<UpdateRequest>(responseString);

                        if (!string.IsNullOrEmpty(updateRequest.RequestError))
                        {
                            var awaiter = new ManualResetEvent(false);

                            var messageBoxWrapper = new ShowMessageBoxDialogWrapper
                            {
                                Awaiter = awaiter,
                                Title = "Update error",
                                Message = "An error occured while checking for online updates. Update canceled."
                            };

                            _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>()
                                .Publish(messageBoxWrapper);
                            return;
                        }


                        var updateVersions = updateRequest.UpdateVersions;

                        if (updateVersions != null && updateVersions.Any())
                        {
                            var currentVersionSettings = _environmentService.GetEnvironmentInfo("SoftwareVersion") as string;
                            if (currentVersionString == "0.0.0.0" && updateVersions.All(x => new Version(x.Version) < new Version(currentVersionSettings)))
                            {
                                updateChannel = "Development";

                                var updateRequest2 = new UpdateRequest
                                {
                                    ActivationKey = license.ActivationKey,
                                    CurrentVersion = currentVersion,
                                    Environment = updateChannel,
                                    CpuId = _environmentService.GetUniqueId()
                                };
                                
                                content = new StringContent(JsonConvert.SerializeObject(updateRequest2), Encoding.UTF8,
                                    "application/json");
                                response = await httpClient.PostAsync($"api/update", content);
                                response.EnsureSuccessStatusCode();
                                
                                responseString = await response.Content.ReadAsStringAsync();
                                updateRequest = JsonConvert.DeserializeObject<UpdateRequest>(responseString);
                                
                                if (!string.IsNullOrEmpty(updateRequest.RequestError))
                                {
                                    var awaiter = new ManualResetEvent(false);
                                    
                                    var messageBoxWrapper = new ShowMessageBoxDialogWrapper
                                    {
                                        Awaiter = awaiter,
                                        Title = "Update error",
                                        Message = "An error occured while checking for online updates. Update canceled."
                                    };
                                    
                                    _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>()
                                        .Publish(messageBoxWrapper);
                                    return;
                                }
                                
                                updateVersions = updateRequest.UpdateVersions;
                            }

                            var toRemoves = new List<string>();
                            var orderedUpdateInfos = updateVersions.OrderBy(info => new Version(info.Version)).ToList();
                            foreach(var info in orderedUpdateInfos.ToList())
                            {
                                if(new Version(info.Version) < new Version(currentVersionSettings))
                                {
                                    toRemoves.Add(info.Version);
                                }
                            }

                            foreach(var toRemove in toRemoves)
                            {
                                var toRemoveItem = orderedUpdateInfos.FirstOrDefault(x => x.Version == toRemove);
                                if (toRemoveItem != null)
                                    orderedUpdateInfos.Remove(toRemoveItem);
                            }


                            var forceRestartItem = orderedUpdateInfos.FirstOrDefault(ui => ui.ForceRestart);

                            if (forceRestartItem != null)
                            {
                                var index = orderedUpdateInfos.IndexOf(forceRestartItem) + 1;

                                var length = orderedUpdateInfos.Count;

                                for (var i = index; i < length; i++)
                                {
                                    orderedUpdateInfos.RemoveAt(i);
                                }
                            }

                            if (orderedUpdateInfos.Any())
                            {
                                var newVersion = orderedUpdateInfos.Last().Version;
                                var confirmationResult = true;

                                if (confirmationRequired)
                                {
                                    confirmationResult = await Task.Factory.StartNew(() =>
                                    {
                                        var awaiter = new ManualResetEvent(false);

                                        var messageBoxWrapper =
                                            new ShowMessageBoxDialogWrapper
                                            {
                                                Awaiter = awaiter,
                                                Title = "UpdateDialog_Title",
                                                Message = "UpdateDialog_Content",
                                                MessageParameter = new[]
                                                    {currentVersionString, newVersion}
                                            };

                                        if (orderedUpdateInfos.Any(uv => uv.ForceRestart))
                                        {
                                            messageBoxWrapper.Message = "UpdateDialog_Content_Incremental";
                                        }

                                        _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>()
                                            .Publish(messageBoxWrapper);

                                        var success = false;

                                        if (awaiter.WaitOne())
                                        {
                                            success = messageBoxWrapper.Result;
                                        }

                                        return success;
                                    });
                                }

                                if (confirmationResult)
                                {
                                    var updateRequest2 = new UpdateRequest
                                    {
                                        ActivationKey = license.ActivationKey,
                                        CurrentVersion = new Version(newVersion),
                                        Environment = updateChannel,
                                        CpuId = _environmentService.GetUniqueId()
                                    };

                                    var content2 = new StringContent(JsonConvert.SerializeObject(updateRequest2),
                                        Encoding.UTF8,
                                        "application/json");
                                    var response2 = await httpClient.PostAsync($"api/update/info", content2);
                                    response2.EnsureSuccessStatusCode();

                                    var responseString2 = await response2.Content.ReadAsStringAsync();

                                    var updateRequest3 = JsonConvert.DeserializeObject<UpdateRequest>(responseString2);

                                    if (!string.IsNullOrEmpty(updateRequest3.RequestError))
                                    {
                                        //TODO:
                                        //return;
                                    }

                                    var showProgressWrapper = new ShowProgressDialogWrapper
                                    {
                                        Title = "ProgressBox_OnlineUpdate_Title",
                                        Message = "ProgressBox_OnlineUpdate_Message"
                                    };

                                    var updateProgress =
                                        _localizationService.GetLocalizedString(
                                            "ProgressBox_OnlineUpdate_Message_Downloading");

                                    showProgressWrapper.MessageParameter = new[] {string.Format(updateProgress, "")};
                                    showProgressWrapper.IsFinished = false;

                                    _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>()
                                        .Publish(showProgressWrapper);

                                    string mainTempFile = null;
                                    foreach (var updateVersion in orderedUpdateInfos)
                                    { 
                                        var tempFiles = new List<string>();

                                            mainTempFile = Path.GetTempFileName();
                                            using (response = await httpClient.GetAsync(
                                                $@"api/update/2/{updateRequest.Environment}/{updateVersion.Version}/main",
                                                HttpCompletionOption.ResponseHeadersRead))
                                            {
                                                response.EnsureSuccessStatusCode();

                                                if (!response.IsSuccessStatusCode)
                                                {
                                                    if (outerProgress != null)
                                                    {
                                                        outerProgress.Report(
                                                            "Unable to download update file. Operation timed out. Your internet connection might be to slow.");
                                                    }
                                                    else
                                                    {
                                                        var awaiter = new ManualResetEvent(false);

                                                        var messageBoxWrapper =
                                                            new ShowMessageBoxDialogWrapper
                                                            {
                                                                Awaiter = awaiter,
                                                                Title = "Update error",
                                                                Message =
                                                                    "Unable to download update file. Operation timed out. Your internet connection might be to slow."
                                                            };

                                                        _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>()
                                                            .Publish(messageBoxWrapper);
                                                    }

                                                    return;
                                                }

                                                var totalDouble = -1d;
                                                var total = response.Content.Headers.ContentLength ?? -1L;
                                                if (total != -1L)
                                                {
                                                    totalDouble = total / 1048576d;
                                                }

                                                IProgress<long> progress = new Progress<long>(value =>
                                                {
                                                    if (outerProgress != null)
                                                    {
                                                        outerProgress.Report(string.Format(
                                                            _localizationService.GetLocalizedString(
                                                                "ProgressBox_OnlineUpdate_Message_Downloading"),
                                                            $"({(value / 1048576d):N2} MB/{totalDouble:N2} MB)"));
                                                    }
                                                    else
                                                    {
                                                        showProgressWrapper.MessageParameter[0] = string.Format(
                                                            _localizationService.GetLocalizedString(
                                                                "ProgressBox_OnlineUpdate_Message_Downloading"),
                                                            $"({(value / 1048576d):N2} MB/{totalDouble:N2} MB)");

                                                        _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>()
                                                            .Publish(showProgressWrapper);
                                                    }
                                                });

                                                using (var contentStream = await response.Content.ReadAsStreamAsync())
                                                using (var fileStream = new FileStream(mainTempFile, FileMode.Create,
                                                    FileAccess.Write))
                                                {
                                                    var task = contentStream.CopyToAsync(fileStream, progress);
                                                    if (await Task.WhenAny(task, Task.Delay(TimeSpan.FromMinutes(5))) !=
                                                        task)
                                                    {
                                                        throw new Exception("Unable to download file main.zip");
                                                    }
                                                }

                                                tempFiles.Add(mainTempFile);
                                            }
                                        

                                        var filesToDeleteList = new List<string>(updateVersion.FilesToDelete);
                                        foreach (var addOn in license.AddOns)
                                        {
                                            var tempFile = Path.GetTempFileName();

                                            response = await httpClient.GetAsync(
                                                $@"api/update/2/{updateRequest.Environment}/{updateVersion.Version}/{addOn}");
                                            response.EnsureSuccessStatusCode();

                                            using (var contentStream = await response.Content.ReadAsStreamAsync())
                                            using (var fileStream = new FileStream(tempFile, FileMode.Create,
                                                FileAccess.Write))
                                            {
                                                await contentStream.CopyToAsync(fileStream);
                                            }

                                            tempFiles.Add(tempFile);

                                            switch (addOn)
                                            {
                                                case "adAuth":
                                                case "localAuth":
                                                    filesToDeleteList.Add("OLS.Casy.Core.Authorization.Default.dll");
                                                    break;
                                                case "simulator":
                                                    filesToDeleteList.Add("OLS.Casy.Com.dll");
                                                    break;
                                                case "network":
                                                    filesToDeleteList.Add("OLS.Casy.IO.SQLite.EF.dll");
                                                    filesToDeleteList.Add("OLS.Casy.IO.SQLite.EF.dll.config");
                                                    break;
                                            }
                                        }

                                        updateVersion.FilesToDelete = filesToDeleteList.ToArray();
                                        updateVersion.TempFiles = tempFiles.ToArray();

                                        if (updateVersion.ForceRestart)
                                        {
                                            break;
                                        }
                                    }

                                    showProgressWrapper.MessageParameter[0] += "\n";
                                    showProgressWrapper.MessageParameter[0] += string.Format(
                                        _localizationService.GetLocalizedString(
                                            "ProgressBox_OnlineUpdate_Message_Extracting"));

                                    _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>()
                                        .Publish(showProgressWrapper);

                                    foreach (var updateVersion in orderedUpdateInfos)
                                    {
                                        if(string.IsNullOrEmpty(mainTempFile))
                                        {
                                            mainTempFile = updateVersion.TempFiles.FirstOrDefault();
                                        }
                                        
                                        updateVersion.UpdateDirectory =
                                            Path.Combine(Path.GetDirectoryName(mainTempFile),
                                                Path.GetFileNameWithoutExtension(mainTempFile)) +
                                            Path.DirectorySeparatorChar;

                                        foreach (var tempFile in updateVersion.TempFiles)
                                        {
                                            ExtractZipFile(tempFile, string.Empty, updateVersion.UpdateDirectory);
                                        }

                                        if (updateVersion.ForceRestart)
                                        {
                                            break;
                                        }
                                    }

                                    var applicationPath = AppDomain.CurrentDomain.BaseDirectory;

                                    foreach (var updateInfo in orderedUpdateInfos)
                                    {
                                        var dirInfo = new DirectoryInfo(updateInfo.UpdateDirectory);
                                        var files = dirInfo.GetFiles();

                                        var updaterUiFiles = files.Where(file =>
                                                Path.GetFileNameWithoutExtension(file.Name)
                                                    .StartsWith(UpdaterExeNamespace))
                                            .ToArray();
                                        foreach (var updaterUiFile in updaterUiFiles)
                                        {
                                            var origFilePath = Path.Combine(applicationPath, updaterUiFile.Name);
                                            var tempFileName = $"{origFilePath}.temp";

                                            var tempFile = new FileInfo(origFilePath);
                                            tempFile.MoveTo(tempFileName);

                                            //_fileSystemStorageService.GetProcessesLockingFile(tempFileName);

                                            await _fileSystemStorageService.CopyFileAsync(updaterUiFile.FullName,
                                                origFilePath);
                                        }

                                        if (updateInfo.ForceRestart)
                                        {
                                            break;
                                        }
                                    }

                                    await Task.Factory.StartNew(async () =>
                                    {
                                        var result =
                                            await _generalMeasureResultManager.SaveChangedMeasureResults();
                                        if (result != ButtonResult.Cancel)
                                        {
                                        }
                                    });

                                    _databaseStorageService.SaveSetting("ShowReleaseNodes", "1");

                                    showProgressWrapper.MessageParameter[0] += "\n";
                                    showProgressWrapper.MessageParameter[0] += string.Format(
                                        _localizationService.GetLocalizedString(
                                            "ProgressBox_OnlineUpdate_Message_ShutingDown"));

                                    Thread.Sleep(500);

                                    showProgressWrapper.IsFinished = true;
                                    _eventAggregatorProvider.Instance.GetEvent<ShowProgressEvent>()
                                        .Publish(showProgressWrapper);

                                    Thread.Sleep(500);

                                    Process process = new Process
                                    {
                                        StartInfo =
                                        {
                                            FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                                "OLS.Casy.Core.Update.Ui.exe")
                                        }
                                    };

                                    var updateInfoFile = Path.GetTempFileName();
                                    File.WriteAllText(updateInfoFile, JsonConvert.SerializeObject(orderedUpdateInfos));

                                    string[] args =
                                    {
                                        "\"" + updateInfoFile + "\"",
                                        "\"" + AppDomain.CurrentDomain.BaseDirectory + "\\\"",
                                        "\"" + Process.GetCurrentProcess().Id + "\""
                                    };

                                    process.StartInfo.Arguments = string.Join(" ", args);
                                    process.StartInfo.Verb = "runas";
                                    process.Start();

                                    Application.Current.Dispatcher.Invoke(() => { Application.Current.Shutdown(); });
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        File.WriteAllText("temp.txt", ex.Message);
                         //ignored
                    }
                }
            }
        } 
        

        public void ExtractZipFile(string archiveFilenameIn, string password, string outFolder)
        {
            ZipFile zf = null;
            try
            {
                var fs = File.OpenRead(archiveFilenameIn);
                zf = new ZipFile(fs);
                if (!string.IsNullOrEmpty(password))
                {
                    zf.Password = password;     // AES encrypted entries are handled automatically
                }
                foreach (ZipEntry zipEntry in zf)
                {
                    if (!zipEntry.IsFile)
                    {
                        continue;           // Ignore directories
                    }
                    var entryFileName = zipEntry.Name;
                    // to remove the folder from the entry:- entryFileName = Path.GetFileName(entryFileName);
                    // Optionally match entrynames against a selection list here to skip as desired.
                    // The unpacked length is available in the zipEntry.Size property.

                    byte[] buffer = new byte[4096];     // 4K is optimum
                    var zipStream = zf.GetInputStream(zipEntry);

                    // Manipulate the output filename here as desired.
                    var fullZipToPath = Path.Combine(outFolder, entryFileName);
                    var directoryName = Path.GetDirectoryName(fullZipToPath);
                    if (directoryName.Length > 0)
                        Directory.CreateDirectory(directoryName);

                    // Unzip file in buffered chunks. This is just as fast as unpacking to a buffer the full size
                    // of the file, but does not waste memory.
                    // The "using" will close the stream even if an exception occurs.
                    using (var streamWriter = File.Create(fullZipToPath))
                    {
                        StreamUtils.Copy(zipStream, streamWriter, buffer);
                    }
                }
            }
            finally
            {
                if (zf != null)
                {
                    zf.IsStreamOwner = true; // Makes close also shut the underlying stream
                    zf.Close(); // Ensure we release resources
                }
            }
        }
    }
}
