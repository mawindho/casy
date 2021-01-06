using System;
using System.Runtime.Serialization;

namespace OLS.Casy.Models
{
    [Serializable]
    public class Cursor : ModelBase
    {
        [NonSerialized]
        private int _cursorId = -1;
        private string _name;
        private double _minLimit;
        private double _maxLimit;
        private string _color;
        private string _createdBy;

        [OptionalField]
        private DateTimeOffset _createdAtOffset;
        //Deprecated, just for older imports
        [OptionalField]
        private DateTime _createdAt;

        private string _lastModifiedBy;

        [OptionalField]
        private DateTimeOffset _lastModifiedAtOffset;
        //Deprecated, just for older imports
        [OptionalField]
        private DateTime _lastModifiedAt;

        private bool _isDeadCellsCursor;

        [NonSerialized]
        private bool _isVisible = true;

        [OptionalField]
        private string _subpopulation;

        [NonSerialized]
        private double _oldMinLimit;
        [NonSerialized]
        private double _oldMaxLimit;
        [OptionalField]
        private int _version;

        public bool IsDirty { get; set; }

        public Cursor()
        {
            IsDirty = true;
        }

        public int CursorId
        {
            get { return _cursorId; }
            set { this._cursorId = value; }
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

        public double MinLimit
        {
            get { return _minLimit; }
            set
            {
                this._minLimit = value;
                IsDirty = true;
                NotifyOfPropertyChange();
                //this._oldMinLimit = _minLimit;
            }
        }

        public double MaxLimit
        {
            get { return _maxLimit; }
            set
            {
                this._maxLimit = value;
                IsDirty = true;
                NotifyOfPropertyChange();
                //this._oldMaxLimit = _maxLimit;
            }
        }

        public double OldMinLimit
        {
            get { return _oldMinLimit; }
            set { this._oldMinLimit = value; }
        }

        public double OldMaxLimit
        {
            get { return _oldMaxLimit; }
            set { this._oldMaxLimit = value; }
        }

        public string Color
        {
            get { return _color; }
            set
            {
                this._color = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsDeadCellsCursor
        {
            get { return _isDeadCellsCursor; }
            set
            {
                this._isDeadCellsCursor = value;
                NotifyOfPropertyChange();
            }
        }

        public MeasureSetup MeasureSetup { get; set; }

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

        public bool IsDelete { get; set; }

        public bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                this._isVisible = value;
                IsDirty = true;
                NotifyOfPropertyChange();
            }
        }

        public string Subpopulation
        {
            get { return _subpopulation; }
            set
            {
                this._subpopulation = value;
                IsDirty = true;
                NotifyOfPropertyChange();
            }
        }

        public override bool Equals(object other)
        {
            Cursor otherCursor = other as Cursor;

            return otherCursor != null && otherCursor.CursorId == this.CursorId && otherCursor.Name == this.Name && otherCursor.MinLimit == this.MinLimit && otherCursor.MaxLimit == this.MaxLimit;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                // Maybe nullity checks, if these are objects not primitives!
                hash = hash * 23 + _cursorId.GetHashCode();
                hash = hash * 23 + _name.GetHashCode();
                hash = hash * 23 + _minLimit.GetHashCode();
                hash = hash * 23 + _maxLimit.GetHashCode();
                return hash;
            }
        }

        [OnDeserializing]
        void OnDeserializing(StreamingContext c)
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
