using OLS.Casy.Core.Logging.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.Models.Enums;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OLS.Casy.IO
{
    /// <summary>
    /// Implementation of <see cref="IFileSystemStorageService"/>.
    /// Uses a queueing mechanism to guarentee the success of the operation if it's failing some time due to windows security reasons
    /// </summary>
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IFileSystemStorageService))]
    public class FileSystemStorageService : IFileSystemStorageService, IDisposable
    {
        private readonly ILogger _logger;
        private readonly ConcurrentQueue<FileSystemTask> _queue;
        private readonly Timer _retryTimeout;
        private readonly object _lock = new object();

        /// <summary>
        /// MEF importing constructor
        /// </summary>
        /// <param name="logger">Implementation od <see cref="ILogger"/> </param>
        [ImportingConstructor]
        public FileSystemStorageService(ILogger logger)
        {
            this._retryTimeout = new Timer(this.DequeueProcess, null, Timeout.Infinite, Timeout.Infinite);
            this._queue = new ConcurrentQueue<FileSystemTask>();
            this._logger = logger;
        }

        public List<Process> GetProcessesLockingFile(string filePath)
        {
            return Win32Processes.WhoIsLocking(filePath);
        }

        public string[] GetFiles(string directoryPath)
        {
            return Directory.GetFiles(directoryPath);
        }

        /// <summary>
        /// Creates async a file with the passed path and binary data
        /// </summary>
        /// <param name="path">Filepath of the file to be created</param>
        /// <param name="data">Binary data of the newly created file</param>
        public Task CreateFileAsync(string path, byte[] data)
        {
            var tcs = new TaskCompletionSource<byte[]>();
            var task = new FileSystemTask
            {
                Type = FileSystemTaskType.Create,
                Data = data,
                DestinationPath = path,
                TaskCompletionSource = tcs
            };

            this._queue.Enqueue(task);

            lock (_lock)
            {
                if (_retryTimeout != null)
                {
                    this._retryTimeout.Change(0, Timeout.Infinite);
                }
            }

            return tcs.Task;
        }

        /// <summary>
        /// Copys async a file from source to destination path
        /// </summary>
        /// <param name="sourcePath">Source path</param>
        /// <param name="destinationPath">Destination path</param>
        public Task CopyFileAsync(string sourcePath, string destinationPath)
        {
            var tcs = new TaskCompletionSource<byte[]>();

            var task = new FileSystemTask
            {
                Type = FileSystemTaskType.Copy,
                SourcePath = sourcePath,
                DestinationPath = destinationPath,
                TaskCompletionSource = tcs
            };

            this._queue.Enqueue(task);

            lock (_lock)
            {
                this._retryTimeout.Change(0, Timeout.Infinite);
            }
            return tcs.Task;
        }

        /// <summary>
        /// Deletes async the file with the passed path
        /// </summary>
        /// <param name="sourcePath">Path of the file to delete</param>
        public Task<bool> DeleteFileAsync(string sourcePath)
        {
            var tcs = new TaskCompletionSource<bool>();

            if (!File.Exists(sourcePath))
            {
                tcs.SetResult(true); // file is already deleted?!
                return tcs.Task;
            }

            var task = new FileSystemTask
            {
                Type = FileSystemTaskType.Delete,
                SourcePath = sourcePath,
                TaskCompletionSource = tcs
            };

            this._queue.Enqueue(task);

            lock (_lock)
            {
                this._retryTimeout.Change(0, Timeout.Infinite);
            }

            return tcs.Task;
        }

        /// <summary>
        /// Reads async the binary content of the file with the passed path
        /// </summary>
        /// <param name="path">Path of the file to be read</param>
        /// <returns>Binary content of the file</returns>
        public Task<byte[]> ReadFileAsync(string path)
        {
            var tcs = new TaskCompletionSource<byte[]>();
            var task = new FileSystemTask { Type = FileSystemTaskType.Read, SourcePath = path, TaskCompletionSource = tcs };

            this._queue.Enqueue(task);

            lock (_lock)
            {
                this._retryTimeout.Change(0, Timeout.Infinite);
            }

            return tcs.Task;
        }

        /// <summary>
        /// Reads async the XML content of the file with the passed path
        /// </summary>
        /// <param name="fileName">Path od the file to be read</param>
        /// <returns>XML content of the file</returns>
        public Task<string> ReadXmlFileAsync(string fileName)
        {
            string result = string.Empty;
            var file = new StreamReader(fileName, Encoding.UTF8, false);
            return Task<string>.Factory.StartNew(() =>
            {
                using (file)
                {
                    result = file.ReadToEnd();
                    file.Close();
                }
                return result;
            });
        }

        /// <summary>
        /// Deletes async the directory with the passed path
        /// </summary>
        /// <param name="sourcePath">Path of the directory to be delete</param>
        /// <param name="recursive">Delete recursivly or not</param>
        /// <returns>True, if the deletion was successful, false otherwise</returns>
        public async Task<bool> DeleteDirectoryAsync(string sourcePath, bool recursive)
        {
            try
            {
                if (!Directory.Exists(sourcePath))
                {
                    throw new ArgumentException("sourcePath");
                }

                await
                    Task.Factory.StartNew(path => Directory.Delete((string)path, recursive),
                                          Path.GetDirectoryName(sourcePath));
                return true;
            }
            catch (Exception e)
            {
                this._logger.Error(LogCategory.File, string.Format("Error while deleting '{0}'\n{1}", sourcePath, e.Message),
                                   () => this.DeleteDirectoryAsync(sourcePath, recursive));
                return false;
            }
        }

        /// <summary>
        /// Creates async the directory with the passed path
        /// </summary>
        /// <param name="name">Path of the directory to be created</param>
        /// <returns>True, if the creation was successful, false otherwise</returns>
        public async Task<bool> CreateDirectoryAsync(string name)
        {
            try
            {
                if (!Directory.Exists(name))
                {
                    await Task.Factory.StartNew(() => { Directory.CreateDirectory(name); });
                }

                return true;
            }
            catch (Exception e)
            {
                this._logger.Error(LogCategory.File, string.Format("Error while creating the directory '{0}'\n{1}", name, e.Message),
                                   () => this.CreateDirectoryAsync(name));
                return false;
            }

        }

        /// <summary>
        /// Creates async a file with the passed path and XML data
        /// </summary>
        /// <param name="path">Path of the file to be created</param>
        /// <param name="data">XML data of the newly created file</param>
        public Task CreateXmlFileAsync(string fileName, string content)
        {
            return Task.Factory.StartNew(() =>
            {
                var file = new StreamWriter(fileName, false, Encoding.UTF8);
                file.WriteLine(content);
                file.Close();
            });
        }

        private void DequeueProcess(object iTimeoutState)
        {
            FileSystemTask task;

            do
            {
                task = null;

                if (this._queue.Count > 0)
                {
                    this._queue.TryDequeue(out task);
                }

                if (task != null)
                {
                    switch (task.Type)
                    {
                        case FileSystemTaskType.Copy:
                            this.CopyTaskProcess(task);
                            break;
                        case FileSystemTaskType.Create:
                            this.CreateTaskProcess(task);
                            break;
                        case FileSystemTaskType.Delete:
                            this.DeleteTaskProcess(task);
                            break;
                        case FileSystemTaskType.Read:
                            this.ReadTaskProcess(task);
                            break;
                    }
                }
            } while (task != null);
        }

        private async void CreateTaskProcess(FileSystemTask task)
        {
            FileStream fileStream = null;

            try
            {
                var fileInfo = new FileInfo(task.DestinationPath);

                if (!fileInfo.Directory.Exists)
                {
                    fileInfo.Directory.Create();
                }

                fileStream = File.Create(task.DestinationPath);
                await fileStream.WriteAsync(task.Data, 0, task.Data.Length);
            }
            catch (Exception)
            {
                this.CreateRetry(task);
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                }

                var temp = (TaskCompletionSource<byte[]>)task.TaskCompletionSource;
                if (!temp.Task.IsCompleted)
                {
                    temp.SetResult(null);
                }
            }
        }

        private async void ReadTaskProcess(FileSystemTask task)
        {
            FileStream fileStream = null;
            byte[] result = null;
            try
            {
                fileStream = File.OpenRead(task.SourcePath);

                result = new byte[fileStream.Length];
                await fileStream.ReadAsync(result, 0, (int)fileStream.Length);
            }
            catch (Exception)
            {
                this.CreateRetry(task);
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Close();

                    var temp = (TaskCompletionSource<byte[]>)task.TaskCompletionSource;
                    if (!temp.Task.IsCompleted)
                    {
                        temp.SetResult(result);
                    }
                }
            }
        }

        private async void CopyTaskProcess(FileSystemTask task)
        {
            FileStream sourceStream = null;
            FileStream destStream = null;

            try
            {
                using (sourceStream = File.OpenRead(task.SourcePath))
                {
                    using (destStream = File.OpenWrite(task.DestinationPath))
                    {
                        await sourceStream.CopyToAsync(destStream);
                    }
                }
            }
            catch (Exception)
            {
                this.CreateRetry(task);
            }
            finally
            {
                if (sourceStream != null)
                {
                    sourceStream.Close();
                }

                if (destStream != null)
                {
                    destStream.Close();
                }

                var temp = (TaskCompletionSource<byte[]>)task.TaskCompletionSource;
                if (!temp.Task.IsCompleted)
                {
                    temp.SetResult(null);
                }
            }
        }

        private async void DeleteTaskProcess(FileSystemTask task)
        {
            try
            {
                var fileInfo = new FileInfo(task.SourcePath);
                await Task.Factory.StartNew(fileInfo.Delete);
                ((TaskCompletionSource<bool>)task.TaskCompletionSource).SetResult(true);
            }
            catch (Exception)
            {
                this.CreateRetry(task);
            }
        }

        private void CreateRetry(FileSystemTask task)
        {
            task.RetryCount++;

            if (task.RetryCount >= 10)
            {
                //TODO: After a to be specified retry count, delete task and throw exception?
                //TODO: Exception werfen?
                throw new IOException("10 retries have been failed. FileSystemTask: " + task);
            }

            this._logger.Debug(LogCategory.File, "Exception occured while file system task execution. Retry #" + task.RetryCount,
                                () => this.CreateRetry(task));

            //In order to avoid instant retries on several problematic tasks you probably 
            //should involve a mechanism to delay retries. Keep in mind that this approach
            //delays worker thread that is implicitly involved by await keyword.
            Thread.Sleep(100);

            this._queue.Enqueue(task);
            lock (_lock)
            {
                this._retryTimeout.Change(0, Timeout.Infinite);
            }
        }

        private class FileSystemTask
        {
            public FileSystemTaskType Type { get; set; }

            public object TaskCompletionSource { get; set; }

            public string SourcePath { get; set; }
            public string DestinationPath { get; set; }

            public byte[] Data { get; set; }

            public long RetryCount { get; set; }

            public override string ToString()
            {
                return string.Format("Type: {0} - Source Path: {1} - Destination Path: {2}", this.Type.ToString(),
                                    this.SourcePath,
                                    this.DestinationPath);
            }
        }

        private enum FileSystemTaskType
        {
            Read,
            Create,
            Copy,
            Delete
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this._retryTimeout.Dispose();
                }

                disposedValue = true;
            }
        }

        ~FileSystemStorageService() {
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
