using OLS.Casy.Base;
using OLS.Casy.Models.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace OLS.Casy.Models
{
    [Serializable]
    public class MeasureSetup : ModelBase
    {
        const double VOL_CONV = Math.PI / 6d;

        [NonSerialized] private int _measureSetupId = -1;
        private string _name;
        [OptionalField]
        private int _version;
        [OptionalField]
        private DateTimeOffset _lastModifiedAtOffset;
        //Deprecated, just for older imports
        [OptionalField]
        private DateTime _lastModifiedAt;
        private string _lastModifiedBy;
        [OptionalField]
        private DateTimeOffset _createdAtOffset;
        //Deprecated, just for older imports
        [OptionalField]
        private DateTime _createdAt;
        private string _createdBy;
        private MeasureModes _measureMode;
        private int _capillarySize;
        private int _fromDiameter;
        private int _toDiameter;
        private Volumes _volume;
        private double _volumeCorrectionFactor;
        private int _repeats;
        private double _dilutionFactor;
        private double _dilutionSampleVolume = 100;
        private double _dilutionCasyTonVolume = 10;
        private AggregationCalculationModes _aggregationCalculationMode;
        private double _manualAggregationCalculationFactor;
        private bool _isSmoothing;
        private double _smoothingFactor;
        private bool _isDeviationControlEnabled;
        private ScalingModes _scalingMode;
        private int _scalingMaxRange;
        private UnitModes _unitMode;
        private bool _isTemplate;
        private string _internalResultItemTypes;
        private string _defaultExperiment;
        private string _defaultGroup;
        private bool _isAutoSave;
        private string _autoSaveName;
        private bool _isAutoComment;
        private int _channelCount;

        private ICollection<AuditTrailEntry> _auditTrailsEntries;
        private ObservableCollection<DeviationControlItem> _deviationControlItems;
        private ObservableCollection<Cursor> _cursors;
        private bool _isReadOnly;

        [NonSerialized] private double[] _smoothDiameters;

        [NonSerialized] private double[] _volumeMapping;

        [NonSerialized] private bool _isManual;

        [OptionalField]
        private bool _hasSubpopulations;

        public MeasureSetup()
        {
            _cursors = new ObservableCollection<Cursor>();
            _deviationControlItems = new ObservableCollection<DeviationControlItem>();
            _auditTrailsEntries = new List<AuditTrailEntry>();
            _channelCount = 1024;
            IsDirty = true;
            Version = 1;
        }

        public bool IsDirty { get; set; }

        public int MeasureSetupId
        {
            get { return _measureSetupId; }
            set { this._measureSetupId = value; }
        }

        public string Name
        {
            get { return _name; }
            set
            {
                this._name = value;
                NotifyOfPropertyChange();
            }
        }

        public int Version
        {
            get { return _version; }
            set
            {
                this._version = value;
                NotifyOfPropertyChange();
            }
        }

        public MeasureModes MeasureMode
        {
            get { return _measureMode; }
            set
            {
                this._measureMode = value;
                IsDirty = true;
                NotifyOfPropertyChange();
            }
        }

        public virtual int CapillarySize
        {
            get { return this._capillarySize; }
            set
            {
                this._capillarySize = value;
                NotifyOfPropertyChange();
            }
        }

        public int FromDiameter
        {
            get { return _fromDiameter; }
            set
            {
                this._fromDiameter = value;
                CalcSmoothedDiametersAndVolumeMapping();
            }
        }

        public virtual int ToDiameter
        {
            get { return _toDiameter; }
            set
            {
                this._toDiameter = value;
                CalcSmoothedDiametersAndVolumeMapping();
                NotifyOfPropertyChange();
            }
        }

        public Volumes Volume
        {
            get { return _volume; }
            set
            {
                this._volume = value;
                IsDirty = true;
                NotifyOfPropertyChange();
            }
        }

        public double VolumeCorrectionFactor
        {
            get { return _volumeCorrectionFactor; }
            set { this._volumeCorrectionFactor = value; }
        }

        public int Repeats
        {
            get { return this._repeats; }
            set
            {
                this._repeats = value;
                IsDirty = true;
                NotifyOfPropertyChange();
            }
        }

        public double DilutionFactor
        {
            get { return _dilutionFactor; }
            set
            {
                this._dilutionFactor = value;
                IsDirty = true;
                NotifyOfPropertyChange();
            }
        }

        public double DilutionSampleVolume
        {
            get { return _dilutionSampleVolume; }
            set
            {
                this._dilutionSampleVolume = value;
                NotifyOfPropertyChange();
            }
        }

        public double DilutionCasyTonVolume
        {
            get { return _dilutionCasyTonVolume; }
            set
            {
                this._dilutionCasyTonVolume = value;
                NotifyOfPropertyChange();
            }
        }

        public AggregationCalculationModes AggregationCalculationMode
        {
            get { return _aggregationCalculationMode; }
            set
            {
                this._aggregationCalculationMode = value;
                IsDirty = true;
                NotifyOfPropertyChange();
            }
        }

        public double ManualAggregationCalculationFactor
        {
            get { return this._manualAggregationCalculationFactor; }
            set
            {
                this._manualAggregationCalculationFactor = value;
                IsDirty = true;
                NotifyOfPropertyChange();
            }
        }

        public bool IsSmoothing
        {
            get { return _isSmoothing; }
            set
            {
                this._isSmoothing = value;
                NotifyOfPropertyChange();
            }
        }

        public double SmoothingFactor
        {
            get { return _smoothingFactor; }
            set
            {
                this._smoothingFactor = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsDeviationControlEnabled
        {
            get { return _isDeviationControlEnabled; }
            set
            {
                this._isDeviationControlEnabled = value;
                NotifyOfPropertyChange();
            }
        }

        public void AddDeviationControlItem(DeviationControlItem deviationControlItem)
        {
            lock (((ICollection)_deviationControlItems).SyncRoot)
            {
                _deviationControlItems.Add(deviationControlItem);
            }
        }

        public void RemoveDeviationControlItem(DeviationControlItem deviationControlItem)
        {
            lock (((ICollection)_deviationControlItems).SyncRoot)
            {
                _deviationControlItems.Remove(deviationControlItem);
            }
        }

        public ObservableCollection<DeviationControlItem> DeviationControlItems
        {
            get
            {
                lock (((ICollection)_deviationControlItems).SyncRoot)
                {
                    return _deviationControlItems;
                }
            }
        }

        public ICollection<AuditTrailEntry> AuditTrailEntries
        {
            get
            {
                lock (((ICollection)_auditTrailsEntries).SyncRoot)
                {
                    return _auditTrailsEntries;
                }
            }
        }

        public ScalingModes ScalingMode
        {
            get { return this._scalingMode; }
            set
            {
                this._scalingMode = value;
                NotifyOfPropertyChange();
            }
        }

        public int ScalingMaxRange
        {
            get { return _scalingMaxRange; }
            set
            {
                this._scalingMaxRange = value;
                NotifyOfPropertyChange();
            }
        }

        public UnitModes UnitMode
        {
            get { return _unitMode; }
            set
            {
                this._unitMode = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsTemplate
        {
            get { return _isTemplate; }
            set { this._isTemplate = value; }
        }

        public string DefaultExperiment
        {
            get { return _defaultExperiment; }
            set
            {
                this._defaultExperiment = value;
                NotifyOfPropertyChange();
            }
        }

        public string DefaultGroup
        {
            get { return _defaultGroup; }
            set
            {
                this._defaultGroup = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsAutoSave
        {
            get { return _isAutoSave; }
            set
            {
                this._isAutoSave = value;
                NotifyOfPropertyChange();
            }
        }

        public string AutoSaveName
        {
            get { return _autoSaveName; }
            set
            {
                this._autoSaveName = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsAutoComment
        {
            get { return _isAutoComment; }
            set
            {
                this._isAutoComment = value;
                NotifyOfPropertyChange();
            }
        }

        public int ChannelCount
        {
            get { return _channelCount; }
            set
            {
                this._channelCount = value;
                CalcSmoothedDiametersAndVolumeMapping();
            }
        }

        public bool IsReadOnly
        {
            get { return _isReadOnly; }
            set { this._isReadOnly = value; }
        }

        public string ResultItemTypes
        {
            get { return _internalResultItemTypes; }
            set
            {
                this._internalResultItemTypes = value;
                NotifyOfPropertyChange();
            }
        }

        public void AddCursor(Cursor cursor)
        {
            lock (((ICollection)_cursors).SyncRoot)
            {
                _cursors.Add(cursor);
            }
        }

        public void RemoveCursor(Cursor cursor)
        {
            lock (((ICollection)_cursors).SyncRoot)
            {
                _cursors.Remove(cursor);
            }
        }

        public void ClearCursors()
        {
            lock (((ICollection)_cursors).SyncRoot)
            {
                _cursors.Clear();
            }
        }

        public void RemoveCursorAt(int index)
        {
            lock (((ICollection)_cursors).SyncRoot)
            {
                _cursors.RemoveAt(index);
            }
        }

        public ObservableCollection<Cursor> Cursors
        {
            get
            {
                lock (((ICollection)_cursors).SyncRoot)
                {
                    return _cursors;
                }
            }
        }

        public string CreatedBy
        {
            get { return _createdBy; }
            set { this._createdBy = value; }
        }

        public DateTimeOffset CreatedAt
        {
            get { return _createdAtOffset; }
            set { this._createdAtOffset = value; }
        }

        public string LastModifiedBy
        {
            get { return _lastModifiedBy; }
            set { this._lastModifiedBy = value; }
        }

        public DateTimeOffset LastModifiedAt
        {
            get { return _lastModifiedAtOffset; }
            set { this._lastModifiedAtOffset = value; }
        }

        public bool IsDeletedSetup { get; set; }

        public MeasureResult MeasureResult { get; set; }

        public double[] SmoothedDiameters
        {
            get { return _smoothDiameters; }
        }

        public double[] VolumeMapping
        {
            get { return _volumeMapping; }
        }

        public bool IsManual
        {
            get { return _isManual; }
            set { this._isManual = value; }
        }

        public bool HasSubpopulations
        {
            get { return _hasSubpopulations; }
            set
            {
                this._hasSubpopulations = value;
                IsDirty = true;
                NotifyOfPropertyChange();
            }
        }

        public void CalcSmoothedDiametersAndVolumeMapping()
        {
            if (ToDiameter > 0)
            {
                if (this.ToDiameter < this.FromDiameter)
                {
                    throw new InvalidOperationException(
                        "Invalid constallation: ToDiameter is smaller then FromDiameter");
                }

                if (ChannelCount < 0)
                {
                    throw new ArgumentOutOfRangeException("ChannelCount", ChannelCount,
                        "Invalid ChannelCount: " + ChannelCount);
                }

                this._smoothDiameters = new double[this.ChannelCount];
                this._volumeMapping = new double[this.ChannelCount];

                double slope = (ToDiameter - FromDiameter) / (double) _volumeMapping.Length;

                for (int i = 0; i < _smoothDiameters.Length; i++)
                {
                    double mue = ToDiameter - FromDiameter;
                    _smoothDiameters[i] =
                        Calculations.CalcSmoothedDiameter(FromDiameter, ToDiameter, i, this.ChannelCount);

                    mue = FromDiameter + slope * ((double) i + 0.5d);
                    _volumeMapping[i] = i == 0 ? 0d : (Math.Pow(mue, 3) * VOL_CONV);
                }
            }
            else
            {
                this._smoothDiameters = null;
                this._volumeMapping = null;
            }
        }

        [OnDeserializing]
        void OnDeserializing(StreamingContext c)
        {
            if (_cursors == null)
            {
                _cursors = new ObservableCollection<Cursor>();
            }

            if (_deviationControlItems == null)
            {
                _deviationControlItems = new ObservableCollection<DeviationControlItem>();
            }

            if (_auditTrailsEntries == null)
            {
                _auditTrailsEntries = new List<AuditTrailEntry>();
            }
        }

        public void Migrate()
        {
            if (_createdAt != default(DateTime))
            {
                _createdAtOffset = new DateTimeOffset(_createdAt);
                _createdAt = default(DateTime);
            }

            if (_lastModifiedAt != default(DateTime))
            {
                _lastModifiedAtOffset = new DateTimeOffset(_lastModifiedAt);
                _lastModifiedAt = default(DateTime);
            }
        }
    }
}
