using OLS.Casy.Core.Api;
using OLS.Casy.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace OLS.Casy.Ui.Core.Api
{
    public interface IMeasureResultManager
    {
        IList<MeasureResult> SelectedMeasureResults { get; }
        Task AddSelectedMeasureResults(IList<MeasureResult> measureResults);
        Task RemoveSelectedMeasureResults(IList<MeasureResult> measureResults);
        void MoveSelectedMeasureResult(MeasureResult itemToMove, MeasureResult movePositionItem, int draggableOverLocation);
        Task ReplaceSelectedMeasureResult(MeasureResult oldMeasureResult, MeasureResult newMeasureResult);
        MeasureResult MeanMeasureResult { get; set; }
        MeasureSetup OverlayMeasureSetup { get; set; }
        event EventHandler<MeasureResultDataChangedEventArgs> SelectedMeasureResultDataChangedEvent;
        event NotifyCollectionChangedEventHandler SelectedMeasureResultsChanged;
        event EventHandler ExperimentsGroupsMappingsChangedEvent;

        IEnumerable<string> GetExperiments(bool includeDeleted = false);
        IEnumerable<string> GetGroups(string experiment, bool includeDeleted = false);

        Task<ButtonResult> SaveMeasureResults(IEnumerable<MeasureResult> measureResults, string defaultName = null, string defaultExperiment = null, string defaultGroup = null, bool cloneSetup = false, bool isSaveAllAllowed = false, bool showConfirmationScreen = true, bool storeAuditTrail = false, bool keepAuditTrail = false);
        Task<bool> DeleteMeasureResults(IList<MeasureResult> measureResult, bool isShutDown = false);
        bool MeasureResultHasChanges(MeasureResult measureResult);
        Task<ButtonResult> SaveChangedMeasureResults(IEnumerable<MeasureResult> measureResults = null, bool allowAcceptWithoutChanges = true, bool isShutDown = false);
        double GetMinMeasureLimit(int capillarySize);
        void StartRangeModificationTimer(MeasureSetup measureSetup);
        void StopRangeModificationTimer(MeasureSetup measureSetup);
        MeasureSetup CloneTemplate(MeasureSetup template);
        string FindMeasurementName(MeasureResult measureResult);
    }
}
