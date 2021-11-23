using OLS.Casy.Controller.Api;
using OLS.Casy.Core;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Config.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using OLS.Casy.Ui.Core.Api;

namespace OLS.Casy.Controller.Measure
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IMeasureResultStorageService))]
    [Export(typeof(IService))]
    public class MeasureResultStorageService : AbstractService, IMeasureResultStorageService
    {
        private readonly IDatabaseStorageService _databaseStorageService;
        private readonly ITemplateManager _templateManager;

        //private readonly IList<MeasureResult> _measureResults;

        [ImportingConstructor]
        public MeasureResultStorageService(
            IConfigService configService,
            IDatabaseStorageService databaseStorageService,
            ITemplateManager templateManager)
            : base(configService)
        {
            this._databaseStorageService = databaseStorageService;
            _templateManager = templateManager;

            //this._measureResults = new List<MeasureResult>();
        }

        public event EventHandler MeasureResultsChangedEvent;

        //public IEnumerable<MeasureResult> MeasureResults
        //{
            //get { return _measureResults; }
        //}

        public IEnumerable<MeasureResult> GetMeasureResults(string experiment, string group, string filter = "", bool includeDeleted = false, bool nullAsNoValue = false, int maxItems = -1)
        {
            return _databaseStorageService.GetMeasureResults(experiment, group, filter, includeDeleted, nullAsNoValue, maxItems);
            //lock ((((ICollection)_measureResults).SyncRoot)))
            //{
            //return includeDeleted ? _measureResults.ToList() : _measureResults.Where(mr => !mr.IsDelete).ToList();
            //}

            //return _databaseStorageService.GetMeasureResults(
            //user == null ? null : $"{user.FirstName} {user.LastName} ({user.Identity.Name})",
            //from, to, includeDeleted);
        }

        /// <summary>
        /// Returns a <see cref="MeasureResult"/> by its ID.
        /// </summary>
        /// <param name="id">ID of the <see cref="MeasureResult"/></param>
        /// <returns>The <see cref="MeasureResult"/> with the passed ID, or NULL if it does not exist</returns>
        public MeasureResult GetMeasureResultById(int id, bool isDeleted = false)
        {
            var result = _databaseStorageService.GetMeasureResult(id, isDeleted: isDeleted);
            if (result != null)
            {
                result = _databaseStorageService.LoadDisplayData(result);
            }
            return result;
        }

        public void StoreMeasureResults(IEnumerable<MeasureResult> measureResults, bool cloneOriginalSetup = false,
            bool keepAuditTrail = false)
        {
            foreach (var measureResult in measureResults)
            {
                if (measureResult == null)
                {
                    throw new ArgumentException("measureResult");
                }

                _databaseStorageService.SaveMeasureSetup(measureResult.MeasureSetup, keepAuditTrail);

                if (cloneOriginalSetup && measureResult.OriginalMeasureSetup.MeasureSetupId != -1)
                {
                    var setupClone =
                        _databaseStorageService.GetMeasureSetup(measureResult.OriginalMeasureSetup.MeasureSetupId,
                            true);
                    setupClone.MeasureSetupId = -1;
                    setupClone.IsTemplate = false;

                    setupClone.MeasureResult = measureResult;

                    foreach (var cursor in setupClone.Cursors)
                    {
                        cursor.CursorId = -1;
                        cursor.MeasureSetup = setupClone;
                    }

                    _databaseStorageService.SaveMeasureSetup(setupClone, keepAuditTrail);

                    measureResult.OriginalMeasureSetup = setupClone;
                }
                else if (measureResult.OriginalMeasureSetup.MeasureSetupId == -1)
                {
                    _databaseStorageService.SaveMeasureSetup(measureResult.OriginalMeasureSetup, keepAuditTrail);
                }
                else if(measureResult.MeasureSetup.MeasureSetupId == measureResult.OriginalMeasureSetup.MeasureSetupId)
                {
                    MeasureSetup newTemplate = null;
                    _templateManager.CloneSetup(measureResult.MeasureSetup, ref newTemplate);
                    measureResult.OriginalMeasureSetup = newTemplate;
                    _databaseStorageService.SaveMeasureSetup(measureResult.OriginalMeasureSetup, keepAuditTrail);
                }

                _databaseStorageService.SaveMeasureResult(measureResult, keepAuditTrail, keepAuditTrail);

                //var existing =
                    //_measureResults.FirstOrDefault(mr => mr.MeasureResultId == measureResult.MeasureResultId);
                //if (existing != null)
                //{
                    //_measureResults.Remove(existing);
                //}
                //_measureResults.Add(measureResult);
            }
            RaiseMeasureResultsChanged();
        }

        public void DeleteMeasureResults(IList<MeasureResult> measureResults)
        {
            if (measureResults == null)
            {
                throw new ArgumentException("measureResult");
            }
            
            _databaseStorageService.DeleteMeasureResults(measureResults);

            foreach (var measureResult in measureResults)
            {
                measureResult.IsDeletedResult = true;
                //measureResult.IsDelete = true;

                //var existing =
                //_measureResults.FirstOrDefault(mr => mr.MeasureResultId == measureResult.MeasureResultId);
                //if (existing != null)
                //{
                //_measureResults.Remove(existing);
                //}
            }

            RaiseMeasureResultsChanged();
        }

        public void RemoveDeletedMeasureResult(int deletedMeasureResultId)
        {
            _databaseStorageService.RemoveDeletedMeasureResult(deletedMeasureResultId);
        }

        /// <summary>
        ///     Pre-condition: MEF has satisfied all references.
        ///     This  method can be used to initialize the service and perform actions, which do
        ///     not expect other dependent services with OnReady state.
        /// </summary>
        public override void Prepare(IProgress<string> progress)
        {
            base.Prepare(progress);
            _databaseStorageService.OnDatabaseReadyEvent += OnDatabaseReady;

            if (_databaseStorageService.IsDatabaseReady)
            {
                OnDatabaseReady(null, null);
            }
        }

        public bool MeasureResultExists(string name, string experiment, string group)
        {
            return this._databaseStorageService.MeasureResultExists(name, experiment, group);
        }

        private void RaiseMeasureResultsChanged()
        {
            if (MeasureResultsChangedEvent != null)
            {
                foreach (EventHandler receiver in MeasureResultsChangedEvent.GetInvocationList())
                {
                    receiver.BeginInvoke(this, EventArgs.Empty, null, null);
                }
            }
        }

        private void OnDatabaseReady(object sender, EventArgs e)
        {
            if (_databaseStorageService.IsDatabaseReady)
            {
                //var measureResults = _databaseStorageService.GetMeasureResults();

                //foreach (var measureResult in measureResults)
                //{
                    //this._databaseStorageService.LoadDisplayData(measureResult);
                    //this._measureResults.Add(measureResult);
                //}
                RaiseMeasureResultsChanged();
            }
        }
    }
}
