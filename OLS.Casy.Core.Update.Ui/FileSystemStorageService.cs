using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OLS.Casy.Core.Update.Ui
{
    public class FileSystemStorageService : IDisposable
    {
        //private readonly UpdateLogger _logger;
        private readonly ConcurrentQueue<FileSystemTask> _queue;
        private readonly Timer _retryTimeout;
        private readonly object _lock = new object();

        public FileSystemStorageService(UpdateLogger logger)
        {
            this._retryTimeout = new Timer(this.DequeueProcess, null, Timeout.Infinite, Timeout.Infinite);
            this._queue = new ConcurrentQueue<FileSystemTask>();
            //this._logger = logger;
        }

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

            lock (this._lock)
            {
                this._retryTimeout.Change(0, Timeout.Infinite);
            }
            return tcs.Task;
        }

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

            lock (this._lock)
            {
                this._retryTimeout.Change(0, Timeout.Infinite);
            }

            return tcs.Task;
        }

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
            catch (Exception)
            {
                //this._logger.Error(string.Format("Error while deleting '{0}'\n{1}", sourcePath, e.Message),
                //                   () => this.DeleteDirectoryAsync(sourcePath, recursive));
                return false;
            }
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
                            //this.CreateTaskProcess(task);
                            break;
                        case FileSystemTaskType.Delete:
                            this.DeleteTaskProcess(task);
                            break;
                        case FileSystemTaskType.Read:
                            //this.ReadTaskProcess(task);
                            break;
                    }
                }
            } while (task != null);
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

            //this._logger.Debug("Exception occured while file system task execution. Retry #" + task.RetryCount,
            //                    () => this.CreateRetry(task));

            //In order to avoid instant retries on several problematic tasks you probably 
            //should involve a mechanism to delay retries. Keep in mind that this approach
            //delays worker thread that is implicitly involved by await keyword.
            Thread.Sleep(100);

            this._queue.Enqueue(task);
            lock (this._lock)
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

        ~FileSystemStorageService()
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
    }
}
