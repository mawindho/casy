using OLS.Casy.Controller.Api;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Core.Api;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace OLS.Casy.Ui.Measure.ViewModels
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(PreselectMeasurementNameDialogModel))]
    public class PreselectMeasurementNameDialogModel : DialogModelBase
    {
        private readonly IMeasureResultStorageService _measureResultStorageService;
        private readonly ILocalizationService _localizationService;
        private readonly IDatabaseStorageService _databaseStorageService;
        private readonly IMeasureResultManager _measureResultManager;

        private volatile bool _canOk;
        private string _measureResultName;
        private string _selectedExperiment;
        private string _selectedGroup;

        private readonly Timer _checkNameTimer;

        [ImportingConstructor]
        public PreselectMeasurementNameDialogModel(IMeasureResultStorageService measureResultStorageService,
            IMeasureResultManager measureResultManager,
            ILocalizationService localizationService,
            IDatabaseStorageService databaseStorageService)
        {
            _measureResultStorageService = measureResultStorageService;
            _localizationService = localizationService;
            _databaseStorageService = databaseStorageService;
            _measureResultManager = measureResultManager;
            CheckCanOk();
            ButtonResult = ButtonResult.Cancel;

            _checkNameTimer = new Timer { Interval = TimeSpan.FromMilliseconds(300).TotalMilliseconds };
            _checkNameTimer.Elapsed += OnCheckNameTimer;
        }

        private void OnCheckNameTimer(object sender, ElapsedEventArgs e)
        {
            _checkNameTimer.Stop();
            CheckCanOk();
        }

        public ButtonResult ButtonResult { get; private set; }

        public string MeasureResultName
        {
            get => _measureResultName;
            set
            {
                if (value == _measureResultName) return;
                _measureResultName = value;
                NotifyOfPropertyChange();

                _checkNameTimer.Stop();
                _checkNameTimer.Start();
            }
        }

        public override bool CanOk => _canOk;

        public IEnumerable<string> KnownExperiments
        {
            get => _measureResultManager.GetExperiments().OrderBy(x => x);
        }

        public string SelectedExperiment
        {
            get => _selectedExperiment;
            set
            {
                if (value == _selectedExperiment) return;

                _selectedExperiment = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange("KnownExperiments");
                NotifyOfPropertyChange("KnownGroups");

                _checkNameTimer.Stop();
                _checkNameTimer.Start();
            }
        }

        public IEnumerable<string> KnownGroups
        {
            get => _measureResultManager.GetGroups(SelectedExperiment).OrderBy(x => x);
        }

        public string SelectedGroup
        {
            get => _selectedGroup;
            set
            {
                var newValue = value == string.Empty ? null : value;

                if (newValue == _selectedGroup) return;

                _selectedGroup = newValue;
                NotifyOfPropertyChange();

                _checkNameTimer.Stop();
                _checkNameTimer.Start();
            }
        }

        protected override void OnOk()
        {
            if (string.IsNullOrEmpty(MeasureResultName)) return;

            ButtonResult = ButtonResult.Ok;
            base.OnOk();
        }

        private void CheckCanOk()
        {
            Task.Run(() =>
            {
                try
                {
                    _canOk = true;

                    var validationErrors = new List<string>();
                    if (string.IsNullOrEmpty(MeasureResultName))
                    {
                        validationErrors.Add(_localizationService.GetLocalizedString("ValidationError_MeasureResultNameMustNotBeEmpty"));
                    }
                    else if (_measureResultStorageService.MeasureResultExists(MeasureResultName, SelectedExperiment, SelectedGroup))
                    {
                        validationErrors.Add(_localizationService.GetLocalizedString("ValidationError_MeasureResultNameMustBeUnique"));
                    }
                    else if (MeasureResultName.IndexOfAny(new[] { '/', '\\', ':', '*', '<', '>', '|' }) > -1)
                    {
                        validationErrors.Add(_localizationService.GetLocalizedString("ValidationError_MeasureResultNameMustNotContainSpecialChars"));
                    }

                    if (validationErrors.Count == 0)
                    {
                        _validationErrors?.TryRemove("MeasureResultName", out _);
                        RaiseErrorsChanged("MeasureResultName");
                    }
                    else
                    {
                        _validationErrors?.TryAdd("MeasureResultName", validationErrors.ToList());
                        RaiseErrorsChanged("MeasureResultName");

                        _canOk = false;
                    }

                    NotifyOfPropertyChange("CanOk");
                }
                catch (Exception ex)
                {
                }
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _checkNameTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
