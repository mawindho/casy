using DevExpress.Mvvm.DataAnnotations;
using OLS.Casy.Controller.Api;
using OLS.Casy.Core;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Core.Localization.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.Models;
using OLS.Casy.Models.Enums;
using OLS.Casy.Ui.Base.Api;
using OLS.Casy.Ui.Core.Api;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Timers;
using System.Web;

namespace OLS.Casy.Ui.Analyze.Models
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(MeasureResultModel))]
    public class MeasureResultModel : DynamicObject, IComparable, IComparable<MeasureResultModel>, INotifyPropertyChanged, IFilterable, IPartImportsSatisfiedNotification, IDisposable
    {
        private readonly IDatabaseStorageService _databaseStorageService;
        private readonly IEventAggregatorProvider _eventAggregatorProvider;
        private readonly ILocalizationService _localizationService;
        private readonly IMeasureResultStorageService _measureResultStorageService;
        private readonly IMeasureResultManager _measureResultManager;
        private readonly IEnvironmentService _environmentService;

        private MeasureResult _associatedMeasureResult;
        private Dictionary<string, MeasureResultAnnotation> _annotations;
        private SmartCollection<string> _knownExperiments;
        private SmartCollection<string> _knownGroups;

        private readonly System.Timers.Timer _updateExperimentTimer;
        private readonly System.Timers.Timer _updateGroupTimer;

        private string _experiment;
        private string _group;

        [ImportingConstructor]
        public MeasureResultModel(IDatabaseStorageService databaseStorageService,
            IEventAggregatorProvider eventAggregatorProvider,
            ILocalizationService localizationService,
            IMeasureResultStorageService measureResultStorageService,
            IMeasureResultManager measureResultManager,
            IEnvironmentService environmentService)
        {
            _databaseStorageService = databaseStorageService;
            _eventAggregatorProvider = eventAggregatorProvider;
            _localizationService = localizationService;
            _measureResultStorageService = measureResultStorageService;
            _measureResultManager = measureResultManager;
            _environmentService = environmentService;

            _updateExperimentTimer = new System.Timers.Timer
                { Interval = TimeSpan.FromMilliseconds(500).TotalMilliseconds };
            _updateExperimentTimer.Elapsed += OnUpdateExperimentTimerElapsed;

            _updateGroupTimer = new System.Timers.Timer { Interval = TimeSpan.FromMilliseconds(500).TotalMilliseconds };
            _updateGroupTimer.Elapsed += OnUpdateGroupTimerElapsed;
        }

        [Hidden]
        public MeasureResult AssociatedMeasureResult
        {
            get => _associatedMeasureResult;
            set
            {
                _associatedMeasureResult = value;

                if (_associatedMeasureResult == null) return;
                
                _annotations = new Dictionary<string, MeasureResultAnnotation>();
                foreach(var annotation in _associatedMeasureResult.MeasureResultAnnotations)
                {
                    _annotations.Add(annotation.AnnotationType.AnnottationTypeName, annotation);
                }

                _experiment = _associatedMeasureResult.Experiment;
                _group = _associatedMeasureResult.Group;
            }
        }

        public string Name => _associatedMeasureResult.Name;

        public IEnumerable<string> KnownExperiments => _measureResultManager.GetExperiments().OrderBy(x => x);

        public string Experiment
        {
            get => _experiment;
            set
            {
                if (value == _associatedMeasureResult.Experiment) return;

                var newExperiment = value == string.Empty ? null : value;
                _experiment = newExperiment;

                _updateExperimentTimer.Stop();
                _updateExperimentTimer.Start();
                
                NotifyOfPropertyChange();
            }
        }



        public string Group
        {
            get => _associatedMeasureResult.Group;
            set
            {
                var newValue = value == string.Empty ? null : value;

                if (newValue == _associatedMeasureResult.Group) return;

                _group = newValue;

                _updateGroupTimer.Stop();
                _updateGroupTimer.Start();
                
                NotifyOfPropertyChange();
            }
        }

        public IEnumerable<string> KnownGroups => _measureResultManager.GetGroups(_associatedMeasureResult.Experiment).OrderBy(x => x);

        public bool IsVisible => _associatedMeasureResult.IsVisible;

        public void SetValue(string propertyName, object value)
        {
            TrySetMember(new SetMemberValueBinder(propertyName), value);
        }

        public object GetValue(string propertyName)
        {
            TryGetMember(new GetMemberValueBinder(propertyName), out var value);
            return value;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var name = binder.Name;

            

            switch (name)
            {
                case "Name":
                    result = this.Name;
                    return true;
                case "Experiment":
                    result = Experiment ?? string.Empty;
                    return true;
                case "Group":
                    result = Group ?? string.Empty;
                    return true;
                case "CreatedAt":
                    result = _environmentService.GetDateTimeString(_associatedMeasureResult.CreatedAt.DateTime);
                    //result = $"{_associatedMeasureResult.CreatedAt:dd.MM.yyyy HH:mm:ss} (UTC)";
                    return true;
                case "CreatedBy":
                    result = _associatedMeasureResult.CreatedBy;
                    return true;
                case "LastModifiedAt":
                    var lastModified = new[]
                    {
                        _associatedMeasureResult.MeasureSetup.LastModifiedAt.UtcDateTime,
                        _associatedMeasureResult.LastModifiedAt.UtcDateTime
                    }.Union(_associatedMeasureResult.MeasureSetup.Cursors.Select(c => c.LastModifiedAt.UtcDateTime)).Max();
                    result = _environmentService.GetDateTimeString(lastModified);
                    //result = $"{lastModified:dd.MM.yyyy HH:mm:ss} (UTC)";
                    return true;
                case "LastModifiedBy":
                    result = _associatedMeasureResult.LastModifiedBy;
                    return true;
                case "MeasuredAt":
                    var timeZone = _associatedMeasureResult.MeasuredAtTimeZone == null ? TimeZoneInfo.Local : _associatedMeasureResult.MeasuredAtTimeZone;
                    var isDaylightSaving = timeZone.IsDaylightSavingTime(_associatedMeasureResult.MeasuredAt);

                    if (isDaylightSaving)
                    {
                        var split = timeZone.DisplayName.Split(':');
                        var hours = (int.Parse(split[0].Split('+')[1])+1).ToString("D2");
                        var timeZoneString = $"(UTC+{hours}:{split[1]}";
                        result = $"{_environmentService.GetDateTimeString(_associatedMeasureResult.MeasuredAt, true)} {timeZoneString}";
                        //result = $"{_associatedMeasureResult.MeasuredAt:dd.MM.yyyy HH:mm:ss} {timeZoneString}";
                    }
                    else
                    {
                        result = $"{_environmentService.GetDateTimeString(_associatedMeasureResult.MeasuredAt, true)} {timeZone.DisplayName}";
                        //result = $"{_associatedMeasureResult.MeasuredAt:dd.MM.yyyy HH:mm:ss} {timeZone.DisplayName}";
                    }
                    return true;
                case "TemplateName":
                    result = _associatedMeasureResult.MeasureSetup.Name;
                    return true;
                case "MeasureMode":
                    result = _localizationService.GetLocalizedString(
                        $"MeasureMode_{Enum.GetName(typeof(MeasureModes), _associatedMeasureResult.MeasureSetup.MeasureMode)}_Name");
                    return true;
                case "CapillarySize":
                    result = _associatedMeasureResult.MeasureSetup.CapillarySize;
                    return true;
                case "FromDiameter":
                    result = _associatedMeasureResult.MeasureSetup.FromDiameter;
                    return true;
                case "ToDiameter":
                    result = _associatedMeasureResult.MeasureSetup.ToDiameter;
                    return true;
                case "Volume":
                    result = (int)_associatedMeasureResult.MeasureSetup.Volume;
                    return true;
                case "Repeats":
                    result = _associatedMeasureResult.MeasureSetup.Repeats;
                    return true;
                case "DilutionFactor":
                    result = _associatedMeasureResult.MeasureSetup.DilutionFactor;
                    return true;
                case "AggregationCalculationMode":
                    switch (_associatedMeasureResult.MeasureSetup.AggregationCalculationMode)
                    {
                        case AggregationCalculationModes.On:
                        case AggregationCalculationModes.Off:
                            result = _localizationService.GetLocalizedString(
                                $"AggregationCalculationMode_{Enum.GetName(typeof(AggregationCalculationModes), _associatedMeasureResult.MeasureSetup.AggregationCalculationMode)}_Name");
                            return true;
                        case AggregationCalculationModes.Manual:
                            result = _associatedMeasureResult.MeasureSetup.ManualAggregationCalculationFactor;
                            return true;
                    }
                    result = null;
                    return false;
                case "SmoothingFactor":
                    result = _associatedMeasureResult.MeasureSetup.IsSmoothing ? _associatedMeasureResult.MeasureSetup.SmoothingFactor : 0d;
                    return true;
                case "ScalingMode":
                    result = _localizationService.GetLocalizedString(
                        $"ScalingMode_{Enum.GetName(typeof(ScalingModes), _associatedMeasureResult.MeasureSetup.ScalingMode)}_Name");
                    return true;
                case "ScalingMaxRange":
                    result = _associatedMeasureResult.MeasureSetup.ScalingMaxRange;
                    return true;
                case "UnitMode":
                    result = _localizationService.GetLocalizedString(
                        $"UnitMode_{Enum.GetName(typeof(UnitModes), _associatedMeasureResult.MeasureSetup.UnitMode)}_Name");
                    return true;
            }

            if (_annotations.TryGetValue(name, out var annotation))
            {
                result = annotation.Value;
                return true;
            }

            var nameSplit = name.Split(new[] { "__" }, StringSplitOptions.None);

            if (nameSplit.Length == 2)
            {
                if (Enum.TryParse(nameSplit[0], out MeasureResultItemTypes measureResultItemType))
                {
                    var cursorName = HttpUtility.UrlDecode(nameSplit[1]);

                    MeasureResultItem resultItem;
                    resultItem = string.IsNullOrEmpty(cursorName) ? _associatedMeasureResult.MeasureResultItemsContainers[measureResultItemType].MeasureResultItem : _associatedMeasureResult.MeasureResultItemsContainers[measureResultItemType].CursorItems.FirstOrDefault(x => x.Cursor != null && x.Cursor.Name == cursorName);
                    if (resultItem != null)
                    {
                        switch(resultItem.MeasureResultItemType)
                        {
                            case MeasureResultItemTypes.Concentration:
                                result = _localizationService.GetLocalizedString(resultItem.ResultItemValue == 0 ? "MeasureResult_Concentration_Ok" : "MeasureResult_Concentration_TooHigh");
                                break;
                            case MeasureResultItemTypes.AggregationFactor:
                                if(_associatedMeasureResult.MeasureSetup.AggregationCalculationMode == AggregationCalculationModes.Off)
                                {
                                    result = _localizationService.GetLocalizedString("AggregationCalculationMode_Off_Name");
                                }
                                else
                                {
                                    resultItem = _associatedMeasureResult
                                        .MeasureResultItemsContainers[measureResultItemType].CursorItems
                                        .FirstOrDefault(x => x.Cursor != null && !x.Cursor.IsDeadCellsCursor);
                                    result = resultItem != null ? resultItem.ResultItemValue.ToString(resultItem.ValueFormat) : _localizationService.GetLocalizedString("AggregationCalculationMode_Off_Name");
                                }
                                break;
                            default:
                                result = resultItem.ResultItemValue.ToString(resultItem.ValueFormat);
                                break;
                        }
                            
                        return true;
                    }
                }
            }

            result = string.Empty;
            return false;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (!(value is string stringValue)) return false;

            if (!_annotations.TryGetValue(binder.Name, out var annotation))
            {
                var annotationType = _databaseStorageService.GetAnnotationTypes().FirstOrDefault(a => a.AnnottationTypeName == binder.Name);

                annotation = new MeasureResultAnnotation()
                {
                    AnnotationType = annotationType,
                    MeasureResult = _associatedMeasureResult
                };
                annotation.PropertyChanged += OnAnnotationPropertyChanged;
                annotation.Value = stringValue;
                if (annotationType != null) _annotations.Add(annotationType.AnnottationTypeName, annotation);
                _associatedMeasureResult.MeasureResultAnnotations.Add(annotation);
            }
            else
            {
                annotation.Value = stringValue;
            }

            _databaseStorageService.SaveMeasureResultAnnotation(annotation);
            NotifyOfPropertyChange(binder.Name);

            return true;
        }

        private void OnAnnotationPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == "Value")
            { 
                var measureResultAnnotation = sender as MeasureResultAnnotation;
                NotifyOfPropertyChange(measureResultAnnotation.AnnotationType.AnnottationTypeName);
            }
        }

        public int CompareTo(object obj)
        {
            if (obj == null)
            {
                return 1;
            }
            MeasureResultModel other = obj as MeasureResultModel;
            if (other == null)
            {
                throw new ArgumentException("A MeasureResultModel object is required for comparison.", "obj");
            }
            return this.CompareTo(other);
        }

        public int CompareTo(MeasureResultModel other)
        {
            if (object.ReferenceEquals(other, null))
            {
                return 1;
            }
            // Ratings compare opposite to normal string order, 
            // so reverse the value returned by String.CompareTo.
            return other.AssociatedMeasureResult.MeasureResultId.CompareTo(this.AssociatedMeasureResult.MeasureResultId);
        }

        public static int Compare(MeasureResultModel left, MeasureResultModel right)
        {
            if (object.ReferenceEquals(left, right))
            {
                return 0;
            }
            if (object.ReferenceEquals(left, null))
            {
                return -1;
            }
            return left.CompareTo(right);
        }

        public override bool Equals(object obj)
        {
            MeasureResultModel other = obj as MeasureResultModel; //avoid double casting
            if (object.ReferenceEquals(other, null))
            {
                return false;
            }
            return this.CompareTo(other) == 0;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                // Maybe nullity checks, if these are objects not primitives!
                hash = hash * 23 + this._associatedMeasureResult.MeasureResultId.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(MeasureResultModel left, MeasureResultModel right)
        {
            if (object.ReferenceEquals(left, null))
            {
                return object.ReferenceEquals(right, null);
            }
            return left.Equals(right);
        }
        public static bool operator !=(MeasureResultModel left, MeasureResultModel right)
        {
            return !(left == right);
        }
        public static bool operator <(MeasureResultModel left, MeasureResultModel right)
        {
            return (Compare(left, right) < 0);
        }
        public static bool operator >(MeasureResultModel left, MeasureResultModel right)
        {
            return (Compare(left, right) > 0);
        }

        class SetMemberValueBinder : SetMemberBinder
        {
            public SetMemberValueBinder(string propertyName)
                : base(propertyName, false)
            {
            }
            public override DynamicMetaObject FallbackSetMember(DynamicMetaObject target, DynamicMetaObject value, DynamicMetaObject errorSuggestion)
            {
                return errorSuggestion;
            }
        }

        class GetMemberValueBinder : GetMemberBinder
        {
            public GetMemberValueBinder(string propertyName)
                : base(propertyName, false)
            {
            }

            public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
            {
                return errorSuggestion;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyOfPropertyChange([CallerMemberName] string callerMemberName = "")
        {
            this.NotifyOfPropertyChangeInternal(callerMemberName);
        }

        private void NotifyOfPropertyChangeInternal(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //public void SendUIEdit(object modelObject, string propStr, object newValue, object oldValue)
        //{
        //    UIPropertyUndoItem op = new UIPropertyUndoItem();
        //    op.ModelObject = modelObject;
        //    op.PropertyStr = propStr;
        //    op.NewValue = newValue;
        //    op.OldValue = oldValue;
        //    this._uiProjectManager.Submit(op);
        //}

        //public void SendUIEdit(object modelObject, string propStr, object newValue)
        //{
        //    SendUIEdit(modelObject, propStr, newValue, null);
        //}

        public void OnImportsSatisfied()
        {
            //_eventAggregatorProvider.Instance.GetEvent<MeasureResultStoredEvent>().Subscribe(OnResultStored);
            _measureResultManager.ExperimentsGroupsMappingsChangedEvent += OnExperimentGroupMappingsChanged;

            //Application.Current.Dispatcher.Invoke(() =>
            //{
            //    InputLanguageManager.Current.InputLanguageChanged += OnInputLanguageChanged;
            //});
        }

        private void OnExperimentGroupMappingsChanged(object sender, EventArgs e)
        {
            NotifyOfPropertyChange("KnownExperiments");
            NotifyOfPropertyChange("KnownGroups");
        }

        //private void OnResultStored()
        //{
            //NotifyOfPropertyChange("KnownExperiments");
            //NotifyOfPropertyChange("KnownGroups");
        //}

        private void OnUpdateExperimentTimerElapsed(object sender, ElapsedEventArgs args)
        {
            _updateExperimentTimer.Stop();

            if (!_measureResultStorageService.MeasureResultExists(_associatedMeasureResult.Name, _experiment, _associatedMeasureResult.Group))
            {
                _associatedMeasureResult.Experiment = _experiment;
                NotifyOfPropertyChange("KnownGroups");
                _measureResultStorageService.StoreMeasureResults(new[] { _associatedMeasureResult });
                _eventAggregatorProvider.Instance.GetEvent<MeasureResultStoredEvent>().Publish();
            }
            else
            {
                _experiment = _associatedMeasureResult.Experiment;
                NotifyOfPropertyChange("Experiment");
            }
        }

        private void OnUpdateGroupTimerElapsed(object sender, ElapsedEventArgs args)
        {
            _updateGroupTimer.Stop();

            if (!_measureResultStorageService.MeasureResultExists(_associatedMeasureResult.Name, _associatedMeasureResult.Experiment, _group))
            {
                _associatedMeasureResult.Group = _group;
                _measureResultStorageService.StoreMeasureResults(new[] { _associatedMeasureResult });
                _eventAggregatorProvider.Instance.GetEvent<MeasureResultStoredEvent>().Publish();
            }
            else
            {
                _group = _associatedMeasureResult.Group;
                NotifyOfPropertyChange("Group");
            }
        }

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (this._annotations != null)
                    {
                        foreach (var annotation in this._annotations.Values)
                        {
                            annotation.PropertyChanged -= OnAnnotationPropertyChanged;
                        }
                    }

                    //_eventAggregatorProvider.Instance.GetEvent<MeasureResultStoredEvent>().Unsubscribe(OnResultStored);
                    _measureResultManager.ExperimentsGroupsMappingsChangedEvent -= OnExperimentGroupMappingsChanged;
                    _updateExperimentTimer.Dispose();
                    _updateGroupTimer.Dispose();

                    //Application.Current.Dispatcher.Invoke(() =>
                    //{
                    //    if (InputManager.Current != null)
                    //    {
                    //        InputLanguageManager.Current.InputLanguageChanged -= OnInputLanguageChanged;
                    //    }
                    //});
                }

                disposedValue = true;
            }
        }

        ~MeasureResultModel()
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
