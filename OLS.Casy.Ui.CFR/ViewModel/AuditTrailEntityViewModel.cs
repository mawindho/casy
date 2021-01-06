using OLS.Casy.Core.Localization.Api;
using OLS.Casy.IO.Api;
using OLS.Casy.Models;
using OLS.Casy.Ui.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OLS.Casy.Core.Api;

namespace OLS.Casy.Ui.AuditTrail.ViewModel
{
    public class AuditTrailEntityViewModel : ViewModelBase
    {
        private AuditTrailEntry _auditTrailEntry;
        private readonly ILocalizationService _localizationService;
        private readonly IDatabaseStorageService _databaseStorageService;
        private readonly IEnvironmentService _environmentService;

        public AuditTrailEntityViewModel(AuditTrailEntry auditTrailEntry, 
            ILocalizationService localizationService,
            IDatabaseStorageService databaseStorageService,
            IEnvironmentService environmentService)
        {
            this._auditTrailEntry = auditTrailEntry;
            this._localizationService = localizationService;
            this._databaseStorageService = databaseStorageService;
            _environmentService = environmentService;
        }

        public DateTime DateChanged => this._auditTrailEntry.DateChanged.UtcDateTime;

        public string DateChangedDisplay => _environmentService.GetDateTimeString(_auditTrailEntry.DateChanged.UtcDateTime);

        public string EntityName
        {
            get
            {
                if(this._auditTrailEntry.EntityName.StartsWith("MeasureResultEntity"))
                {
                    return _localizationService.GetLocalizedString("AuditTrailEntry_Name_Measurement");
                }
                else if(this._auditTrailEntry.EntityName.StartsWith("MeasureSetupEntity"))
                {
                    return _localizationService.GetLocalizedString("AuditTrailEntry_Name_Template");
                }
                else if(this._auditTrailEntry.EntityName == "MeasureResultDataEntity")
                {
                    return _localizationService.GetLocalizedString("AuditTrailEntry_Name_MeasurementData");
                }
                else if(this._auditTrailEntry.EntityName.StartsWith("Cursor"))
                {
                    if(!string.IsNullOrEmpty(this._auditTrailEntry.PrimaryKeyValue)) //&&
                        //this._auditTrailEntry.MeasureResult != null &&
                        //this._auditTrailEntry.MeasureResult.MeasureSetup != null &&
                        //this._auditTrailEntry.MeasureResult.MeasureSetup.Cursors != null)
                    {
                        //var cursor = this._auditTrailEntry.MeasureResult.MeasureSetup.Cursors.FirstOrDefault(c => c.CursorId == int.Parse(this._auditTrailEntry.PrimaryKeyValue));
                        var cursor = _databaseStorageService.GetCursor(int.Parse(this._auditTrailEntry.PrimaryKeyValue));

                         if (cursor != null)
                        {
                            return string.Format("{0} ({1})", _localizationService.GetLocalizedString("AuditTrailEntry_Name_Range"), this._localizationService.GetLocalizedString(cursor.Name));
                        }
                    }
                    return _localizationService.GetLocalizedString("AuditTrailEntry_Name_Range");
                }
                return this._auditTrailEntry.EntityName;
            }
        }
        
        public string Action
        {
            get { return this._localizationService.GetLocalizedString(string.Format("AuditTrailEntry_Action_{0}", this._auditTrailEntry.Action)); }
        }

        public string PropertyName
        {
            get { return this._auditTrailEntry.PropertyName; }
        }

        public string OldValue
        {
            get
            {
                double doubleValue;
                if(double.TryParse(this._auditTrailEntry.OldValue, out doubleValue))
                {
                    return doubleValue.ToString("0.00");
                }
                return this._auditTrailEntry.OldValue;
            }
        }

        public string NewValue
        {
            get
            {
                double doubleValue;
                if (double.TryParse(this._auditTrailEntry.NewValue, out doubleValue))
                {
                    return doubleValue.ToString("0.00");
                }
                return this._auditTrailEntry.NewValue;
            }
        }

        public string UserChanged
        {
            get { return this._auditTrailEntry.UserChanged; }
        }

        public string ComputerName
        {
            get { return this._auditTrailEntry.ComputerName; }
        }

        public string SoftwareVersion
        {
            get { return this._auditTrailEntry.SoftwareVersion; }
        }
    }
}
