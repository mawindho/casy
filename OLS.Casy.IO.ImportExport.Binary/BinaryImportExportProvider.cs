using OLS.Casy.Core;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Core.Logging.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.Models;
using OLS.Casy.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using OLS.Casy.Base;

namespace OLS.Casy.IO.ImportExport.Binary
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IBinaryImportExportProvider))]
    public class BinaryImportExportProvider : IBinaryImportExportProvider
    {
        private readonly IFileSystemStorageService _fileSystemStorageService;
        private readonly IDatabaseStorageService _databaseStorageService;
        private readonly IEncryptionProvider _encryptionProvider;
        private readonly IEventAggregatorProvider _eventAggregatorProvider;
        private readonly ICompositionFactory _compositionFactory;
        private readonly ILogger _logger;

        [ImportingConstructor]
        public BinaryImportExportProvider(IFileSystemStorageService fileSystemStorageService,
            IDatabaseStorageService databaseStorageService,
            IEncryptionProvider encryptionProvider,
            IEventAggregatorProvider eventAggregatorProvider,
            ICompositionFactory compositionFactory,
            ILogger logger)
        {
            _fileSystemStorageService = fileSystemStorageService;
            _databaseStorageService = databaseStorageService;
            _encryptionProvider = encryptionProvider;
            _logger = logger;
        }

        public async Task ExportMeasureResultsAsync(IEnumerable<MeasureResult> measureResults, string filePath)
        {
            MeasureResult[] toSerialize = new MeasureResult[measureResults.Count()];

            for (var i = 0; i < measureResults.Count(); i++)
            {
                var measureResult = measureResults.ElementAt(i);
                measureResult = _databaseStorageService.LoadDisplayData(measureResult);
                measureResult = _databaseStorageService.LoadExportData(measureResult);
                
                toSerialize[i] = measureResult;
            }

            await ExportInternalAsync(toSerialize, filePath);

            _logger.Info(LogCategory.ImportExport,
                $"File Export: Successfully exported {measureResults.Count()} measure results to file '{filePath}'. Measure result names: {string.Join("; ", measureResults.Select(mr => mr.Name))}");
        }

        public async Task<IEnumerable<MeasureResult>> ImportMeasureResultsAsync(string filePath)
        {
            IEnumerable<MeasureResult> results = await ImportInternalAsync<IEnumerable<MeasureResult>>(filePath);

            var importMeasureResultsAsync = results.ToList();
            foreach (var measureResult in importMeasureResultsAsync)
            {
                if (measureResult.MeasureSetup.ChannelCount == 0)
                {
                    measureResult.MeasureSetup.ChannelCount = 1024;
                

                

                foreach (var cursor in measureResult.MeasureSetup.Cursors)
                {
                    cursor.MinLimit = Calculations.CalcSmoothedDiameter(0, cursor.MeasureSetup.ToDiameter,
                        (int) cursor.MinLimit,
                        cursor.MeasureSetup.ChannelCount == 0 ? 1024 : cursor.MeasureSetup.ChannelCount);
                    cursor.MaxLimit = Calculations.CalcSmoothedDiameter(0, cursor.MeasureSetup.ToDiameter,
                        (int) cursor.MaxLimit,
                        cursor.MeasureSetup.ChannelCount == 0 ? 1024 : cursor.MeasureSetup.ChannelCount);
                }
            }

                measureResult.MeasureSetup.Migrate();

                foreach (var cursor in measureResult.MeasureSetup.Cursors)
                {
                    cursor.Migrate();
                }

                foreach (var measureResultAuditTrailEntry in measureResult.AuditTrailEntries)
                    {
                        measureResultAuditTrailEntry.Migrate();
                    }
                
                if(measureResult.Origin == null)
                {
                    measureResult.Origin = string.Empty;
                }

                if (measureResult.MeasuredAtTimeZone == null)
                {
                    measureResult.MeasuredAtTimeZone = TimeZoneInfo.Local;
                }

                measureResult.Migrate();
            }
            
            _logger.Info(LogCategory.ImportExport,
                $"File Import: Successfully imported {importMeasureResultsAsync.Count} measure results from file '{filePath}'. Measure result names: {string.Join("; ", importMeasureResultsAsync.Select(mr => mr.Name))}");
            return importMeasureResultsAsync;
        }

        public async Task ExportMeasureSetupAsync(MeasureSetup measureSetup, string filePath)
        {
            await ExportInternalAsync(measureSetup, filePath);
            _logger.Info(LogCategory.ImportExport,
                $"File Export: Successfully exported measure result '{measureSetup.Name}' to file '{filePath}'.");
        }

        public async Task<MeasureSetup> ImportMeasureSetupAsync(string filePath)
        {
            MeasureSetup measureSetup = await ImportInternalAsync<MeasureSetup>(filePath);
            _logger.Info(LogCategory.ImportExport,
                $"File Import: Successfully imported measure result '{measureSetup.Name}' from file '{filePath}'.");
            return measureSetup;
        }

        private async Task ExportInternalAsync<T>(T toSerialize, string filePath)
        {
            using (var memoryStream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(memoryStream, toSerialize);

                memoryStream.Seek(0, SeekOrigin.Begin);

                var bytes = new byte[memoryStream.Length];
                memoryStream.Read(bytes, 0, (int)memoryStream.Length);

                bytes = _encryptionProvider.Encrypt(bytes);

                try
                {
                    await _fileSystemStorageService.CreateFileAsync(filePath, bytes);
                }
                catch (IOException ioException)
                {
                    await Task.Factory.StartNew(() =>
                    {
                        var awaiter = new ManualResetEvent(false);

                        var messageBoxWrapper = new ShowMessageBoxDialogWrapper
                        {
                            Awaiter = awaiter,
                            Title = "Error while export",
                            Message = $"The system wasn't able to create the selected file '{filePath}'"
                        };

                        _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>().Publish(messageBoxWrapper);

                        awaiter.WaitOne();

                    });
                }
                
            }
        }

        private async Task<T> ImportInternalAsync<T>(string filePath) where T : class
        {
            var bytes = await _fileSystemStorageService.ReadFileAsync(filePath);

            try
            {
                bytes = _encryptionProvider.Decrypt(bytes);
            }
            catch (Exception e)
            {
                await Task.Factory.StartNew(() =>
                {
                    var awaiter = new ManualResetEvent(false);

                    var messageBoxWrapper = new ShowMessageBoxDialogWrapper
                    {
                        Awaiter = awaiter,
                        Title = "Error while decryption",
                        Message = $"The system wasn't able to decrypt the imported file. Probably the file has been manipulated manually and must be ignored!"
                    };

                    _eventAggregatorProvider.Instance.GetEvent<ShowMessageBoxEvent>().Publish(messageBoxWrapper);

                    awaiter.WaitOne();

                });
            }
            

            using (var memoryStream = new MemoryStream(bytes))
            {
                var formatter = new BinaryFormatter();
                var result = formatter.Deserialize(memoryStream) as T;
                return result;
            }
        }
    }
}
