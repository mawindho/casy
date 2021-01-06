using OLS.Casy.Core.Api;
using OLS.Casy.Core.Authorization.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Core.Logging.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.Models;
using OLS.Casy.Models.Enums;
using OLS.Casy.Ui.Core.Api;
using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace OLS.Casy.Ui.Core
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(ITemplateManager))]
    [Export(typeof(TemplateManager))]
    public class TemplateManager : ITemplateManager
    {
        private readonly IEventAggregatorProvider _eventAggregatorProvider;
        private readonly IDatabaseStorageService _databaseStorageService;
        private readonly IUIProjectManager _uiProject;
        private readonly IAuthenticationService _authenticationService;
        private readonly ILogger _logger;

        [ImportingConstructor]
        public TemplateManager(IEventAggregatorProvider eventAggregatorProvider,
            IDatabaseStorageService databaseStorageService,
            IUIProjectManager uiProject,
            IAuthenticationService authenticationService,
            ILogger logger)
        {
            this._eventAggregatorProvider = eventAggregatorProvider;
            this._databaseStorageService = databaseStorageService;
            this._uiProject = uiProject;
            this._authenticationService = authenticationService;
            this._logger = logger;
        }

        public void DeleteTemplate(MeasureSetup template)
        {
            this._logger.Info(LogCategory.Template, string.Format("Template has been deleted: {0}.", template.Name));
            this._databaseStorageService.DeleteMeasureSetup(template);
        }

        public async Task<bool> SaveTemplate(MeasureSetup template)
        {
            return await Task.Factory.StartNew<bool>(() =>
            {
                var isNewTemplate = string.IsNullOrEmpty(template.Name);

                template.IsTemplate = true;

                if (isNewTemplate)
                {
                    template.MeasureSetupId = -1;

                    var awaiter = new System.Threading.ManualResetEvent(false);

                    ShowInputDialogWrapper showInputWrapper = new ShowInputDialogWrapper()
                    {
                        Awaiter = awaiter,
                        Message = "SaveTemplateAsDialog_Content",
                        Title = "SaveTemplateAsDialog_Title",
                        CanOkDelegate = (input) =>
                        {
                            return !string.IsNullOrEmpty(input) && input.IndexOfAny(new[] { '/', '\\', ':', '*', '<', '>', '|' }) == -1;
                        }
                    };

                    _eventAggregatorProvider.Instance.GetEvent<ShowInputEvent>().Publish(showInputWrapper);

                    if (awaiter.WaitOne() && !showInputWrapper.IsCancel)
                    {
                        var templateName = showInputWrapper.Result;

                        if (string.IsNullOrEmpty(templateName))
                        {
                            return false;
                        }
                        template.Name = templateName;

                    }
                }

                if (isNewTemplate)
                {
                    _logger.Info(LogCategory.Template, string.Format("Template '{0}' has been created.", template.Name));
                }
                else
                {
                    this._logger.Info(LogCategory.Template, string.Format("Template has been modified and stored: {0}.", template.Name));
                }
                _databaseStorageService.SaveMeasureSetup(template);

                _uiProject.Clear();
                //foreach (var cursor in template.Cursors)
                //{
                //    _uiProject.Clear(cursor);
                //}
                //_uiProject.Clear(template);

                //foreach (var deviationControlItem in template.DeviationControlItems)
                //{
                //    _uiProject.Clear(deviationControlItem);
                //}
                _eventAggregatorProvider.Instance.GetEvent<TemplateSavedEvent>().Publish(null);
                return true;
            });
        }

        public void CloneSetup(MeasureSetup measureSetup, ref MeasureSetup newSetup)
        {
            if (newSetup == null)
            {
                newSetup = new MeasureSetup();
                newSetup.MeasureSetupId = -1;
            }
            else
            {
                newSetup.Cursors.Clear();
                newSetup.DeviationControlItems.Clear();
            }

            newSetup.IsTemplate = measureSetup.IsTemplate;
            newSetup.AggregationCalculationMode = measureSetup.AggregationCalculationMode;
            newSetup.AutoSaveName = measureSetup.AutoSaveName;
            newSetup.CapillarySize = measureSetup.CapillarySize;
            newSetup.CreatedAt = DateTime.Now;
            newSetup.CreatedBy = string.Format("{0} {1} ({2})", _authenticationService.LoggedInUser.FirstName, _authenticationService.LoggedInUser.LastName, _authenticationService.LoggedInUser.Identity.Name);
            newSetup.DefaultExperiment = measureSetup.DefaultExperiment;
            newSetup.DefaultGroup = measureSetup.DefaultGroup;
            newSetup.DilutionCasyTonVolume = measureSetup.DilutionCasyTonVolume;
            newSetup.DilutionFactor = measureSetup.DilutionFactor;
            newSetup.DilutionSampleVolume = measureSetup.DilutionSampleVolume;
            newSetup.FromDiameter = measureSetup.FromDiameter;
            newSetup.HasSubpopulations = measureSetup.HasSubpopulations;
            newSetup.IsAutoComment = measureSetup.IsAutoComment;
            newSetup.IsAutoSave = measureSetup.IsAutoSave;
            newSetup.IsDeviationControlEnabled = measureSetup.IsDeviationControlEnabled;
            newSetup.IsManual = measureSetup.IsManual;
            newSetup.IsReadOnly = measureSetup.IsReadOnly;
            newSetup.IsSmoothing = measureSetup.IsSmoothing;
            newSetup.ManualAggregationCalculationFactor = measureSetup.ManualAggregationCalculationFactor;
            newSetup.MeasureMode = measureSetup.MeasureMode;
            newSetup.Name = measureSetup.Name;
            newSetup.Repeats = measureSetup.Repeats;
            newSetup.ResultItemTypes = measureSetup.ResultItemTypes;
            newSetup.ScalingMaxRange = measureSetup.ScalingMaxRange;
            newSetup.ScalingMode = measureSetup.ScalingMode;
            newSetup.SmoothingFactor = measureSetup.SmoothingFactor;
            newSetup.ToDiameter = measureSetup.ToDiameter;
            newSetup.UnitMode = measureSetup.UnitMode;
            newSetup.Volume = measureSetup.Volume;
            newSetup.VolumeCorrectionFactor = measureSetup.VolumeCorrectionFactor;
            newSetup.ChannelCount = measureSetup.ChannelCount;

            foreach (var cursor in measureSetup.Cursors)
            {
                var newCursor = new Casy.Models.Cursor()
                {
                    Color = cursor.Color,
                    CreatedAt = DateTime.Now,
                    CreatedBy = string.Format("{0} {1} ({2})", _authenticationService.LoggedInUser.FirstName, _authenticationService.LoggedInUser.LastName, _authenticationService.LoggedInUser.Identity.Name),
                    CursorId = -1,
                    IsDeadCellsCursor = cursor.IsDeadCellsCursor,
                    IsDelete = cursor.IsDelete,
                    IsVisible = cursor.IsVisible,
                    MaxLimit = cursor.MaxLimit,
                    MinLimit = cursor.MinLimit,
                    MeasureSetup = newSetup,
                    Name = cursor.Name,
                    Subpopulation = cursor.Subpopulation
                };
                newSetup.Cursors.Add(newCursor);
            }

            foreach (var deviationControlItem in measureSetup.DeviationControlItems)
            {
                var newControllItem = new DeviationControlItem()
                {
                    DeviationControlItemId = -1,
                    MaxLimit = deviationControlItem.MaxLimit,
                    MeasureResultItemType = deviationControlItem.MeasureResultItemType,
                    MeasureSetup = newSetup,
                    MinLimit = deviationControlItem.MinLimit
                };
                newSetup.DeviationControlItems.Add(newControllItem);
            }
        }
    }
}
