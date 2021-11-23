using OLS.Casy.Core;
using OLS.Casy.Models;
using OLS.Casy.Models.Enums;
using System;
using System.Collections.Generic;

namespace OLS.Casy.Controller.Api
{
    public interface IMeasureResultStorageService
    {
        event EventHandler MeasureResultsChangedEvent;

        IEnumerable<MeasureResult> GetMeasureResults(string experiment, string group, string filter = "", bool includeDeleted = false, bool nullAsNoValue = false, int maxItems = -1);
        
        /// <summary>
        /// Returns a <see cref="MeasureResult"/> by its ID.
        /// </summary>
        /// <param name="id">ID of the <see cref="MeasureResult"/></param>
        /// <returns>The <see cref="MeasureResult"/> with the passed ID, or NULL if it does not exist</returns>
        MeasureResult GetMeasureResultById(int id, bool isDeleted = false);

        void StoreMeasureResults(IEnumerable<MeasureResult> measureResults, bool cloneOriginalSetup = false,
            bool keepAuditTrail = false);

        void DeleteMeasureResults(IList<MeasureResult> measureResults);
        bool MeasureResultExists(string name, string experiment, string group);
        void RemoveDeletedMeasureResult(int deletedMeasureResultId);
    }
}
