using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using OLS.Casy.Models.Enums;

namespace OLS.Casy.Models
{
    [Serializable]
    public class MeasureResult : ModelBase
    {
        [NonSerialized]
        private int _measureResultId = -1;
        private Guid _measureResultGuid;
        private string _serialNumber;
        private string _comment;
        private string _name;
        private MeasureSetup _measureSetup;
        private MeasureSetup _originalMeasureSetup;
        private string _createdBy;
        [OptionalField]
        private DateTimeOffset _createdAtOffset;
        [OptionalField]
        private DateTimeOffset _lastModifiedAtOffset;
        private string _lastModifiedBy;
        
        private string _color;
        private DateTime _measuredAt;
        private string _origin;

        private ObservableCollection<MeasureResultData> _measureResultDatas;
        private ObservableCollection<MeasureResultAnnotation> _measureResultAnnotations;
        private ICollection<AuditTrailEntry> _auditTrailsEntries;

        [NonSerialized]
        private IReadOnlyDictionary<MeasureResultItemTypes, MeasureResultItemsContainer> _measureResultItems;
        private string _experiment;
        private string _group;

        [NonSerialized]
        private bool _isVisible;

        [NonSerialized]
        private int _displayOrder;

        [NonSerialized]
        private ObservableCollection<MeasureResultAccessMapping> _accessMappings;

        [OptionalField]
        private TimeZoneInfo _measuredAtTimeZone;
        [OptionalField]
        private bool _isCfr;

        [OptionalField]
        private DateTime _createdAt;
        [OptionalField]
        private DateTime _lastModifiedAt;
        [OptionalField]
        private int _version;
        [OptionalField]
        private DateTimeOffset? _lastWeeklyClean;

        public MeasureResult()
        {
            _measureResultDatas = new ObservableCollection<MeasureResultData>();
            _measureResultAnnotations = new ObservableCollection<MeasureResultAnnotation>();
            _auditTrailsEntries = new List<AuditTrailEntry>();
            _accessMappings = new ObservableCollection<MeasureResultAccessMapping>();
            IsTemporary = true;
            IsVisible = true;
            Origin = string.Empty;
            IsDirty = true;
        }

        protected MeasureResult(
           SerializationInfo info,
           StreamingContext context)
            : this()
        {
            this.MeasureResultId = -1;
        }

        public bool IsDirty { get; set; }

        public int MeasureResultId
        {
            get { return _measureResultId; }
            set { _measureResultId = value; }
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

        public Guid MeasureResultGuid
        {
            get { return _measureResultGuid; }
            set { _measureResultGuid = value; }
        }

        public string SerialNumber
        {
            get { return _serialNumber; }
            set { _serialNumber = value; }
        }

        public string Comment
        {
            get { return _comment; }
            set
            {
                this._comment = value;
                NotifyOfPropertyChange();
            }
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

        public string Experiment
        {
            get { return _experiment; }
            set { this._experiment = value; }
        }

        public string Group
        {
            get { return _group; }
            set { this._group = value; }
        }

        public string Color
        {
            get { return _color; }
            set
            {
                _color = value;
                NotifyOfPropertyChange();
            }
        }

        public MeasureSetup MeasureSetup
        {
            get { return _measureSetup; }
            set { this._measureSetup = value; }
        }

        public virtual MeasureSetup OriginalMeasureSetup
        {
            get { return _originalMeasureSetup; }
            set { this._originalMeasureSetup = value; }
        }

        public virtual ObservableCollection<MeasureResultData> MeasureResultDatas
        {
            get
            {
                lock (((ICollection)_measureResultDatas).SyncRoot)
                {
                    return _measureResultDatas;
                }
            }
        }

        public void ForceClearMeasureResultItems()
        {
            _measureResultItems = null;
        }

        public IReadOnlyDictionary<MeasureResultItemTypes, MeasureResultItemsContainer> MeasureResultItemsContainers
        {
            get
            {
                if (_measureResultItems == null)
                {
                    _measureResultItems = new Dictionary<MeasureResultItemTypes, MeasureResultItemsContainer>
                    {
                        { MeasureResultItemTypes.AggregationFactor, new MeasureResultItemsContainer(MeasureResultItemTypes.AggregationFactor) { MeasureResult = this } },
                        { MeasureResultItemTypes.Concentration, new MeasureResultItemsContainer(MeasureResultItemTypes.Concentration) { MeasureResult = this } },
                        { MeasureResultItemTypes.Counts, new MeasureResultItemsContainer(MeasureResultItemTypes.Counts) { MeasureResult = this } },
                        { MeasureResultItemTypes.CountsAboveDiameter, new MeasureResultItemsContainer(MeasureResultItemTypes.CountsAboveDiameter) { MeasureResult = this } },
                        { MeasureResultItemTypes.CountsPerMl, new MeasureResultItemsContainer(MeasureResultItemTypes.CountsPerMl) { MeasureResult = this } },
                        { MeasureResultItemTypes.CountsPercentage, new MeasureResultItemsContainer(MeasureResultItemTypes.CountsPercentage) { MeasureResult = this } },
                        { MeasureResultItemTypes.DebrisCount, new MeasureResultItemsContainer(MeasureResultItemTypes.DebrisCount) { MeasureResult = this } },
                        { MeasureResultItemTypes.DebrisVolume, new MeasureResultItemsContainer(MeasureResultItemTypes.DebrisVolume) { MeasureResult = this } },
                        { MeasureResultItemTypes.Deviation, new MeasureResultItemsContainer(MeasureResultItemTypes.Deviation) { MeasureResult = this } },
                        { MeasureResultItemTypes.MeanDiameter, new MeasureResultItemsContainer(MeasureResultItemTypes.MeanDiameter) { MeasureResult = this } },
                        { MeasureResultItemTypes.MeanVolume, new MeasureResultItemsContainer(MeasureResultItemTypes.MeanVolume) { MeasureResult = this } },
                        { MeasureResultItemTypes.PeakDiameter, new MeasureResultItemsContainer(MeasureResultItemTypes.PeakDiameter) { MeasureResult = this } },
                        { MeasureResultItemTypes.PeakVolume, new MeasureResultItemsContainer(MeasureResultItemTypes.PeakVolume) { MeasureResult = this } },
                        { MeasureResultItemTypes.SubpopulationAPercentage, new MeasureResultItemsContainer(MeasureResultItemTypes.SubpopulationAPercentage) { MeasureResult = this } },
                        { MeasureResultItemTypes.SubpopulationBPercentage, new MeasureResultItemsContainer(MeasureResultItemTypes.SubpopulationBPercentage) { MeasureResult = this } },
                        { MeasureResultItemTypes.SubpopulationCPercentage, new MeasureResultItemsContainer(MeasureResultItemTypes.SubpopulationCPercentage) { MeasureResult = this } },
                        { MeasureResultItemTypes.SubpopulationDPercentage, new MeasureResultItemsContainer(MeasureResultItemTypes.SubpopulationDPercentage) { MeasureResult = this } },
                        { MeasureResultItemTypes.SubpopulationEPercentage, new MeasureResultItemsContainer(MeasureResultItemTypes.SubpopulationEPercentage) { MeasureResult = this } },
                        { MeasureResultItemTypes.SubpopulationACountsPerMl, new MeasureResultItemsContainer(MeasureResultItemTypes.SubpopulationACountsPerMl) { MeasureResult = this } },
                        { MeasureResultItemTypes.SubpopulationBCountsPerMl, new MeasureResultItemsContainer(MeasureResultItemTypes.SubpopulationBCountsPerMl) { MeasureResult = this } },
                        { MeasureResultItemTypes.SubpopulationCCountsPerMl, new MeasureResultItemsContainer(MeasureResultItemTypes.SubpopulationCCountsPerMl) { MeasureResult = this } },
                        { MeasureResultItemTypes.SubpopulationDCountsPerMl, new MeasureResultItemsContainer(MeasureResultItemTypes.SubpopulationDCountsPerMl) { MeasureResult = this } },
                        { MeasureResultItemTypes.SubpopulationECountsPerMl, new MeasureResultItemsContainer(MeasureResultItemTypes.SubpopulationECountsPerMl) { MeasureResult = this } },
                        { MeasureResultItemTypes.TotalCellsPerMl, new MeasureResultItemsContainer(MeasureResultItemTypes.TotalCellsPerMl) { MeasureResult = this } },
                        { MeasureResultItemTypes.TotalCountsPerMl, new MeasureResultItemsContainer(MeasureResultItemTypes.TotalCountsPerMl) { MeasureResult = this } },
                        { MeasureResultItemTypes.Viability, new MeasureResultItemsContainer(MeasureResultItemTypes.Viability) { MeasureResult = this } },
                        { MeasureResultItemTypes.ViableCellsPerMl, new MeasureResultItemsContainer(MeasureResultItemTypes.ViableCellsPerMl) { MeasureResult = this } },
                        { MeasureResultItemTypes.VolumePerMl, new MeasureResultItemsContainer(MeasureResultItemTypes.VolumePerMl) { MeasureResult = this } },
                    };
                }

                return _measureResultItems;
            }
        }

        public ObservableCollection<MeasureResultAnnotation> MeasureResultAnnotations
        {
            get
            {
                lock (((ICollection)_measureResultAnnotations).SyncRoot)
                {
                    return _measureResultAnnotations;
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

        public string CreatedBy
        {
            get { return _createdBy; }
            set
            {
                this._createdBy = value;
                NotifyOfPropertyChange();
            }
        }

        public DateTimeOffset CreatedAt
        {
            get { return _createdAtOffset; }
            set
            {
                this._createdAtOffset = value;
                NotifyOfPropertyChange();
            }
        }

        public string LastModifiedBy
        {
            get { return _lastModifiedBy; }
            set
            {
                this._lastModifiedBy = value;
                NotifyOfPropertyChange();
            }
        }

        public DateTimeOffset LastModifiedAt
        {
            get { return _lastModifiedAtOffset; }
            set
            {
                this._lastModifiedAtOffset = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsTemporary { get; set; }

        public bool IsReadOnly { get; set; }
        public bool IsBackground { get; set; }

        public bool IsVisible
        {
            get { return this._isVisible; }
            set
            {
                if(value != _isVisible)
                {
                    this._isVisible = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public int DisplayOrder
        {
            get { return this._displayOrder; }
            set
            {
                if (value != _displayOrder)
                {
                    this._displayOrder = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public DateTime MeasuredAt
        {
            get { return _measuredAt; }
            set { this._measuredAt = value; }
        }

        public TimeZoneInfo MeasuredAtTimeZone
        {
            get { return _measuredAtTimeZone; }
            set { _measuredAtTimeZone = value; ; }
        }

        public string Origin
        {
            get { return _origin; }
            set { this._origin = value; }
        }

        public ObservableCollection<MeasureResultAccessMapping> AccessMappings
        {
            get
            {
                lock (((ICollection)_accessMappings).SyncRoot)
                {
                    return _accessMappings;
                }
            }
        }

        public bool IsCfr
        {
            get { return this._isCfr; }
            set { this._isCfr = value; }
        }

        public DateTimeOffset? LastWeeklyClean
        {
            get { return _lastWeeklyClean; }
            set
            {
                this._lastWeeklyClean = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsDeletedResult { get; set; }

        [OnDeserializing]
        void OnDeserializing(StreamingContext c)
        {
            if (_accessMappings == null)
            {
                _accessMappings = new ObservableCollection<MeasureResultAccessMapping>();
            }

            if (_auditTrailsEntries == null)
            {
                _auditTrailsEntries = new List<AuditTrailEntry>();
            }

            if (_measureResultAnnotations == null)
            {
                _measureResultAnnotations = new ObservableCollection<MeasureResultAnnotation>();
            }

            if (_measureResultDatas == null)
            {
                _measureResultDatas = new ObservableCollection<MeasureResultData>();
            }

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
