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
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;

namespace OLS.Casy.Ui.Core.ViewModels
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(SaveMeasurementDialogModel))]
    public class SaveMeasurementDialogModel : DialogModelBase 
    {
        private readonly IMeasureResultStorageService _measureResultStorageService;
        private readonly ILocalizationService _localizationService;
        private readonly IDatabaseStorageService _databaseStorageService;
        private readonly IMeasureResultManager _measureResultManager;

        private string _measureResultName;
        private string _comment;
        private string _selectedExperiment;
        private string _selectedGroup;
        private bool _isSaveAll;
        private volatile bool _canOk;

        //private SmartCollection<string> _knownExperiments;
        //private SmartCollection<string> _knownGroups;
        //private readonly object _lock = new object();
        private readonly Timer _checkNameTimer;

        [ImportingConstructor]
        public SaveMeasurementDialogModel(IMeasureResultStorageService measureResultStorageService,
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

        public bool IsCommentRequired { private get; set; }
        public string OriginalName { private get; set; }
        public string OriginalExperiment { private get; set; }
        public string OriginalGroup { private get; set; }
        public bool IsDeleted { private get; set; }

        public ButtonResult ButtonResult { get; private set; }

        public bool IsSaveAll
        {
            get => _isSaveAll;
            set
            {
                if (value == _isSaveAll) return;
                _isSaveAll = value;
                NotifyOfPropertyChange();
            }
        }

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

        public IEnumerable<string> KnownMeasureResultNames
        {
            get { return _measureResultStorageService.GetMeasureResults(SelectedExperiment, SelectedGroup).Select(mr => mr.Name).Distinct().ToList(); }
        }

        public string Comment
        {
            get => _comment;
            set
            {
                if (value == _comment) return;
                _comment = value;
                NotifyOfPropertyChange();

                _checkNameTimer.Stop();
                _checkNameTimer.Start();
            }
        }

        public override bool CanOk => _canOk;

        /*private void CheckCanOk()
        {
            Task.Run(() =>
            {
                lock (_lock))
                {
                    _canOk = !string.IsNullOrEmpty(MeasureResultName) &&
                             MeasureResultName.IndexOfAny(new[] { '/', '\\', ':', '*', '<', '>', '|' }) == -1 &&
                             (!IsCommentRequired || !string.IsNullOrEmpty(Comment)) &&
                             ((OriginalName == MeasureResultName &&
                               OriginalExperiment == SelectedExperiment &&
                               OriginalGroup == SelectedGroup) ||
                              !_measureResultStorageService.MeasureResultExists(MeasureResultName, SelectedExperiment, SelectedGroup));
                }

                NotifyOfPropertyChange("CanOk");
            });
        }*/

        public IEnumerable<string> KnownExperiments
        {
            get => _measureResultManager.GetExperiments().OrderBy(x => x);
            /*
            get
            {
                if (_knownExperiments != null) return _knownExperiments;
                _knownExperiments = new SmartCollection<string>();
                _knownExperiments.AddRange(_databaseStorageService.GetExperiments().Where(e => !string.IsNullOrEmpty(e.Item1)).Select(e => e.Item1).Distinct().OrderBy(experiment => experiment).ToList());
                return _knownExperiments;
            }
            */
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
            /*
            get
            {
               _knownGroups = new SmartCollection<string>();
               
               _knownGroups.AddRange(_databaseStorageService.GetGroups(SelectedExperiment).Where(g => !string.IsNullOrEmpty(g.Item1)).Select(g => g.Item1).Distinct().OrderBy(group => group).ToList());
                return _knownGroups;
            }
            */
        }

        public string SelectedGroup
        {
            get => _selectedGroup;
            set
            {
                var newValue = value == string.Empty ? null : value;

                if (newValue == _selectedGroup) return;
                
                //if (!KnownGroups.Contains(value))
                //{
                    //_knownGroups.Add(value);
                //}

                _selectedGroup = newValue;
                NotifyOfPropertyChange();
                //NotifyOfPropertyChange("KnownGroups");

                _checkNameTimer.Stop();
                _checkNameTimer.Start();
            }
        }

        protected override void OnOk()
        {
            if (string.IsNullOrEmpty(MeasureResultName) ||
                IsCommentRequired && string.IsNullOrEmpty(Comment)) return;
            
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
                    else if (((IsDeleted || !(OriginalName == MeasureResultName && OriginalExperiment == SelectedExperiment && OriginalGroup == SelectedGroup)) && _measureResultStorageService.MeasureResultExists(MeasureResultName, SelectedExperiment, SelectedGroup)))
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

                    //validationErrors = new List<string>();
                    if (IsCommentRequired && string.IsNullOrEmpty(Comment))
                    {
                        validationErrors.Add(_localizationService.GetLocalizedString("ValidationError_CommentIsMendetory"));
                    }

                    if (validationErrors.Count == 0)
                    {
                        _validationErrors?.TryRemove("Comment", out _);
                        RaiseErrorsChanged("Comment");
                    }
                    else
                    {
                        _validationErrors?.TryAdd("Comment", validationErrors);
                        RaiseErrorsChanged("Comment");

                        _canOk = false;
                    }

                    NotifyOfPropertyChange("CanOk");
                } 
                catch(Exception ex)
                {
                }
            });
        }

        public ICommand SaveAllCommand => new OmniDelegateCommand(OnSaveAll);

        private void OnSaveAll()
        {
            if (string.IsNullOrEmpty(MeasureResultName) ||
                (IsCommentRequired && string.IsNullOrEmpty(Comment))) return;
            
            ButtonResult = ButtonResult.SaveAll;
            base.OnOk();
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
