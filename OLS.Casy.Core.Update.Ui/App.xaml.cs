//using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace OLS.Casy.Core.Update.Ui
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string UPDATER_EXE_NAMESPACE = "OLS.Casy.Core.Update.Ui";

        private IProgress<string> _textProgress;
        private IProgress<double> _percentageProgress;
        private List<Tuple<string, string>> _processedFiles;
        private UpdateLogger _updateLogger;
        private FileSystemStorageService _fileSystemStorageService;

        public App()
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssemblies;
        }

        public void App_StartUp(object sender, StartupEventArgs e)
        {
            //Debugger.Launch();

            DispatcherUnhandledException += OnDispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            Current.DispatcherUnhandledException += OnDispatcherUnhandledException;

            var updateInfosFile = e.Args[0];
            //this._updateLogger.Info(updateInfosFile);

            var updateInfos = Newtonsoft.Json.JsonConvert.DeserializeObject<List<UpdateVersion>>(File.ReadAllText(updateInfosFile));

            this._updateLogger = new UpdateLogger(updateInfos.FirstOrDefault().Version);

            var applicationPath = e.Args[1];

            this._updateLogger.Info(applicationPath);
            var processId = e.Args[2];

            this._updateLogger.Info(processId);

            MainViewModel mainViewModel = new MainViewModel();

            this._textProgress = new Progress<string>((s) => mainViewModel.ProgressText = s);
            this._percentageProgress = new Progress<double>((d) => mainViewModel.ProgressValue = d);

            MainWindow mainWindow = new MainWindow();
            mainWindow.DataContext = mainViewModel;
            mainWindow.Show();

            Task.Factory.StartNew(async () =>
            {
                this._fileSystemStorageService = new FileSystemStorageService(this._updateLogger);

                this._updateLogger.Info("Trying to shut down main application. Timout: 10 seconds");
                try
                {
                    Process mainApplicationProcess = Process.GetProcessById(int.Parse(processId));

                    if (!mainApplicationProcess.WaitForExit((int)TimeSpan.FromSeconds(10d).TotalMilliseconds))
                    {
                        mainApplicationProcess.Kill();
                    }
                }
                catch
                {
                }

                var orderedUpdateInfos = updateInfos.DistinctBy(i => i.Version).OrderBy(info => new Version(info.Version));

                this._processedFiles = new List<Tuple<string,string>>();

                try
                {
                    foreach (var updateInfo in orderedUpdateInfos)
                    {
                        this._updateLogger.Info(string.Format("Beginning update. Version: {0}; Update path: {1}; Destination path: {2}", updateInfo.Version.ToString(), updateInfo.UpdateDirectory, applicationPath));

                        DirectoryInfo dirInfo = new DirectoryInfo(updateInfo.UpdateDirectory);
                        await this.ProcessDirectoryInfo(dirInfo, updateInfo, applicationPath);

                        var filesToDelete = updateInfo.FilesToDelete;
                        foreach(var file in filesToDelete)
                        {
                            var filePath = Path.Combine(applicationPath, file);
                            if(File.Exists(filePath))
                            { 
                                await this._fileSystemStorageService.DeleteFileAsync(filePath);
                            }
                        }
                    }

                    //foreach (var fileName in this._processedFiles)
                    //{
                        //if(!string.IsNullOrEmpty(fileName.Item2))
                        //{
                            //var oldFileName = string.Format("{0}.{1}", fileName.Item1, fileName.Item2);
                            //this._updateLogger.Info("Deleting old file: " + oldFileName);

                            //if (File.Exists(oldFileName))
                            //{
                                //await this._fileSystemStorageService.DeleteFileAsync(oldFileName);
                            //}
                        //}
                    //}

                    //DirectoryInfo appDirInfo = new DirectoryInfo(applicationPath);
                    //var tempFiles = appDirInfo.GetFiles("*.temp");
                    //foreach (var file in tempFiles)
                    //{
                        //if (File.Exists(file.FullName))
                        //{
                            //this._updateLogger.Info("Deleting temp file: " + file.Name);
                            //await this._fileSystemStorageService.DeleteFileAsync(file.FullName);
                        //}
                    //}

                    

                    this._processedFiles.Clear();
                    this._textProgress.Report("Update succeded. Starting CASY ...");
                    this._updateLogger.Info("Update succeded. Starting CASY ...");
                }
                catch(Exception ex)
                {
                    this._updateLogger.Info(ex.ToString());

                    this._textProgress.Report("Update failed. Rolling back ...");
                    this._updateLogger.Info("Update failed. Rolling back ...");

                    foreach(var fileName in this._processedFiles)
                    {
                        this._updateLogger.Info("Rolling back file: " + fileName);
                        this._textProgress.Report("Rolling back file: " + fileName);

                        await this._fileSystemStorageService.DeleteFileAsync(fileName.Item1);

                        if (!string.IsNullOrEmpty(fileName.Item2))
                        {
                            FileInfo oldFile = new FileInfo(string.Format("{0}.{1}", fileName.Item1, fileName.Item2));
                            oldFile.MoveTo(fileName.Item1);
                        }
                    }
                }

                //if(deleteUpdateFiles)
                //{
                //    await fileSystemStorageService.DeleteDirectoryAsync(updatePath, true);
                //}

                Process process = new Process();
                process.StartInfo.FileName = "OLS.Casy.AppService.exe";
                process.StartInfo.Verb = "runas";
                process.Start();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Application.Current.Shutdown();
                });
            });
        }

        private Assembly ResolveAssemblies(object sender, ResolveEventArgs args)
        {
            _updateLogger.Info(args.Name);
            //MessageBox.Show(args.Name);
            if (args.Name.StartsWith("Newtonsoft.Json"))
            {
                try
                {
                    // Load from application's base directory. Alternative logic might be needed if you need to 
                    // load from GAC etc. However, note that calling certain overloads of Assembly.Load will result
                    // in the AssemblyResolve event from firing recursively - see recommendations in
                    // http://msdn.microsoft.com/en-us/library/ff527268.aspx for further info
                    var assembly = Assembly.LoadFrom("Newtonsoft.Json.dll");
                    return assembly;
                }
                catch (Exception exception)
                {
                    _updateLogger.Info(exception.Message);
                    Console.WriteLine(exception);
                }
            }
            return null;
        }

        private async Task ProcessDirectoryInfo(DirectoryInfo dirInfo, UpdateVersion updateInfo, string baseDirPath)
        {
            this._updateLogger.Info(baseDirPath);

            var files = dirInfo.GetFiles();

            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];

                if (!Path.GetFileNameWithoutExtension(file.Name).StartsWith(UPDATER_EXE_NAMESPACE))
                {
                    string message = string.Format("Copying file {0}", file.Name);
                    this._updateLogger.Info(message);
                    this._textProgress.Report(message);

                    var origFilePath = Path.Combine(baseDirPath, file.Name);
                    if (File.Exists(origFilePath))
                    {
                        var oldFileName = string.Format("{0}.{1}.old", origFilePath, updateInfo.Version);
                        FileInfo oldFile = new FileInfo(origFilePath);

                        this._updateLogger.Info(string.Format("Moving file {0} to {1}", origFilePath, oldFileName));
                        oldFile.MoveTo(oldFileName);

                        this._processedFiles.Add(new Tuple<string, string>(origFilePath, string.Format("{0}.old", updateInfo.Version)));
                    }
                    else
                    {
                        this._updateLogger.Info(string.Format("Origfile does not exist {0}", origFilePath));
                        this._processedFiles.Add(new Tuple<string, string>(origFilePath, null));
                    }
                    this._updateLogger.Info(string.Format("Copying file {0}", origFilePath));

                    if(!Directory.Exists(origFilePath))
                    {
                        var directory = Directory.CreateDirectory(new FileInfo(origFilePath).DirectoryName);
                    }
                    await CopyNewFile(file, origFilePath, 0);
                    
                    this._percentageProgress.Report(i * 100d / file.Length);
                }
            }

            var subDirectories = dirInfo.GetDirectories();

            foreach(var subDirectory in subDirectories)
            {
                await this.ProcessDirectoryInfo(subDirectory, updateInfo, Path.Combine(baseDirPath, subDirectory.Name));
            }
        }

        private async Task CopyNewFile(FileInfo file, string origFilePath, int count)
        {
            if(count < 5)
            {
                try
                {
                    await this._fileSystemStorageService.CopyFileAsync(file.FullName, origFilePath);
                }
                catch (Exception e)
                {
                    if (count < 5)
                    {
                        _updateLogger.Info("File seems to be locked. Retrying ...");
                        await CopyNewFile(file, origFilePath, ++count);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogUnhandledException(e.ExceptionObject as Exception);
        }

        private static void OnUnobservedException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            LogUnhandledException(e.Exception);
        }

        private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs dispatcherUnhandledExceptionEventArgs)
        {
            LogUnhandledException(dispatcherUnhandledExceptionEventArgs.Exception);
        }

        public static void LogUnhandledException(Exception exception)
        {
            var unhandledExceptionText = new StringBuilder();

            unhandledExceptionText.AppendLine($"Error Report {DateTime.Now:yyyy-MM-dd HH':'mm':'ss}:");

            if (exception != null)
            {

                unhandledExceptionText.AppendLine(
                    $"{DateTime.Now:yyyy-MM-dd HH':'mm':'ss}: Unhandled exception caught: {exception.Message}; {exception.StackTrace};");

                if (exception is ReflectionTypeLoadException le1)
                {
                    foreach (var e in le1.LoaderExceptions)
                    {
                        unhandledExceptionText.AppendLine(
                            $"{DateTime.Now:yyyy-MM-dd HH':'mm':'ss}: Unhandled loader exception: {e.Message}; {e.StackTrace};");
                    }
                }

                exception = exception.InnerException;
                while (exception != null)
                {
                    unhandledExceptionText.AppendLine(
                        $"{DateTime.Now:yyyy-MM-dd HH':'mm':'ss}: Unhandled inner exception: {exception.Message}; {exception.StackTrace};");
                    exception = exception.InnerException;

                    if (!(exception is ReflectionTypeLoadException)) continue;
                    var le = (ReflectionTypeLoadException)exception;

                    foreach (var e in le.LoaderExceptions)
                    {
                        unhandledExceptionText.AppendLine(
                            $"{DateTime.Now:yyyy-MM-dd HH':'mm':'ss}: Unhandled inner loader exception {e.Message}; {e.StackTrace};");
                    }
                }
            }
            else
            {
                unhandledExceptionText.AppendLine("AppService has unhandled exception catched.");
            }

            var path = @"UpdaterException.txt";
            //if (!File.Exists(path))
            //{
            File.WriteAllText(path, unhandledExceptionText + Environment.NewLine);

            //Process.Start("OLS.Casy.ErrorReport.Ui.exe");
            //}
            //else
            //{
            //  File.AppendAllText(path, unhandledExceptionText + Environment.NewLine);
            //}
        }
    }
}
