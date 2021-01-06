using System;
using System.Runtime.Serialization;

namespace OLS.Casy.Models
{
    [Serializable]
    public class AuditTrailEntry
    {
        [NonSerialized] private int _auditTrailEntryId = -1;
        private string _entityName;
        private string _action;
        private string _propertyName;
        private string _primaryKeyValue;
        private string _oldValue;
        private string _newValue;
        [OptionalField] private DateTime _dateChanged;
        [OptionalField] private DateTimeOffset _dateChangedOffset;
        private string _userChanged;
        private string _computerName;
        private string _softwareVersion;

        public int AuditTrailEntryId
        {
            get { return _auditTrailEntryId; }
            set { this._auditTrailEntryId = value; }
        }

        public MeasureResult MeasureResult { get; set; }
        public MeasureSetup Template { get; set; }

        public string EntityName
        {
            get { return _entityName; }
            set { this._entityName = value; }
        }

        public string Action
        {
            get { return _action; }
            set { this._action = value; }
        }

        public string PropertyName
        {
            get { return _propertyName; }
            set { this._propertyName = value; }
        }

        public string PrimaryKeyValue
        {
            get { return _primaryKeyValue; }
            set { this._primaryKeyValue = value; }
        }

        public string OldValue
        {
            get { return _oldValue; }
            set { this._oldValue = value; }
        }

        public string NewValue
        {
            get { return _newValue; }
            set { this._newValue = value; }
        }

        public DateTimeOffset DateChanged
        {
            get { return _dateChangedOffset; }
            set { this._dateChangedOffset = value; }
        }

        public string UserChanged
        {
            get { return _userChanged; }
            set { this._userChanged = value; }
        }

        public string ComputerName
        {
            get { return _computerName; }
            set { this._computerName = value; }
        }

        public string SoftwareVersion
        {
            get { return _softwareVersion; }
            set { this._softwareVersion = value; }
        }

        [OnDeserializing]
        void OnDeserializing(StreamingContext c)
        {
            if (_dateChanged != default(DateTime))
            {
                _dateChangedOffset = new DateTimeOffset(_dateChanged);
                _dateChanged = default(DateTime);
            }
        }

        public void Migrate()
        {
            if (_dateChanged != default(DateTime))
            {
                _dateChangedOffset = new DateTimeOffset(_dateChanged);
                _dateChanged = default(DateTime);
            }
        }
    }
}
