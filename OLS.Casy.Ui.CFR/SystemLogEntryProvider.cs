using OLS.Casy.Core.Logging.Api;
using OLS.Casy.Models;
using OLS.Casy.Ui.Base.Virtualization;
using System;
using System.Collections.Generic;
using OLS.Casy.Core.Api;

namespace OLS.Casy.Ui.AuditTrail
{
    public class SystemLogEntryProvider : IItemsProvider<SystemLogEntry>
    {
        private readonly ILogger _logger;
        private readonly IEnvironmentService _environmentService;

        public SystemLogEntryProvider(ILogger _logger, IEnvironmentService environmentService)
        {
            this._logger = _logger;
            _environmentService = environmentService;
        }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public IEnumerable<int> Categories { get; set; }

        public int FetchCount()
        {
            //if(FromDate != null && ToDate != null)
            //{
                //return _logger.GetSystemLogEntryCount(FromDate.Value, ToDate.Value);
            //}
            return _logger.GetSystemLogEntryCount(FromDate, ToDate, Categories);
        }

        public IList<SystemLogEntry> FetchRange(int startIndex, int count)
        {
            //if (FromDate != null && ToDate != null)
            //{
                //return _logger.GetSystemLogEntries(FromDate.Value, ToDate.Value, startIndex, count);
            //}
            var entries = _logger.GetSystemLogEntries(FromDate, ToDate, Categories, startIndex, count);
            foreach (var entry in entries)
            {
                entry.DateDisplay = _environmentService.GetDateTimeString(entry.Date.DateTime);
            }

            return entries;
        }
    }
}
