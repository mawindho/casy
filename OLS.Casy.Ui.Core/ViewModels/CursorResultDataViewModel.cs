using OLS.Casy.Core.Localization.Api;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Core.Api;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System;

namespace OLS.Casy.Ui.Core.ViewModels
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(CursorResultDataViewModel))]
    public class CursorResultDataViewModel : CursorViewModelBase
    {
        private ILocalizationService _localizationService;

        private string _countsPerMl;
        private string _aggregationFactor;
        private string _volumePerMl;
        private string _peakDiameter;
        private string _meanDiameter;
        private string _peakVolume;
        private string _debriDisplay;
        private bool _isNotReadOnlyMeasureResult = true;
        private string _countsPercentage;
        private bool _isViability;
        private bool _hasSubpopulations;
        private IEnumerable<string> _availableSubPopulations;
        private string _subpopulation;
        private bool _hasSubpopulationA;
        private bool _hasSubpopulationB;
        private bool _hasSubpopulationC;
        private bool _hasSubpopulationD;
        private bool _hasSubpopulationE;
        private string _countsPercentageA;
        private string _countsPercentageB;
        private string _countsPercentageC;
        private string _countsPercentageD;
        private string _countsPercentageE;
        private string _countsPerMlA;
        private string _countsPerMlB;
        private string _countsPerMlC;
        private string _countsPerMlD;
        private string _countsPerMlE;

        [ImportingConstructor]
        public CursorResultDataViewModel(
            IUIProjectManager uiProject,
            ILocalizationService localizationService,
            IMeasureResultManager measureResultManager
            )
            : base(uiProject, measureResultManager)
        {
            this._localizationService = localizationService;
        }

        public override string Name
        {
            get
            {
                return this._localizationService.GetLocalizedString(IsDebris ? "Cursor_Debris_Name" :  base.Name);
            }
            set
            {
              base.Name = value;
            }
        }

        public IEnumerable<string> KnownRangeNames
        {
            get { return base.Cursor == null ? new List<string>(0) : base.Cursor.MeasureSetup.Cursors.Select(c => this._localizationService.GetLocalizedString(c.Name)).Distinct().ToList(); }
        }

        public string CountsPerMl
        {
            get { return _countsPerMl; }
            set
            {
                if(value != _countsPerMl)
                {
                    this._countsPerMl = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public string AggregationFactor
        {
            get { return _aggregationFactor; }
            set
            {
                if (value != _aggregationFactor)
                {
                    this._aggregationFactor = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public string CountsPercentage
        {
            get { return _countsPercentage; }
            set
            {
                if (value != _countsPercentage)
                {
                    this._countsPercentage = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public string VolumePerMl
        {
            get { return _volumePerMl; }
            set
            {
                if (value != _volumePerMl)
                {
                    this._volumePerMl = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public string PeakDiameter
        {
            get { return _peakDiameter; }
            set
            {
                if (value != _peakDiameter)
                {
                    this._peakDiameter = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public string MeanDiameter
        {
            get { return _meanDiameter; }
            set
            {
                if (value != _meanDiameter)
                {
                    this._meanDiameter = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public string PeakVolume
        {
            get { return _peakVolume; }
            set
            {
                if (value != _peakVolume)
                {
                    this._peakVolume = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public string DebriDisplay
        {
            get { return _debriDisplay; }
            set
            {
                if (value != _debriDisplay)
                {
                    this._debriDisplay = value;
                    NotifyOfPropertyChange();
                    NotifyOfPropertyChange("IsDebris");
                }
            }
        }

        public bool IsNotReadOnlyMeasureResult
        {
            get { return _isNotReadOnlyMeasureResult; }
            set
            {
                if (value != _isNotReadOnlyMeasureResult)
                {
                    _isNotReadOnlyMeasureResult = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public bool IsViability
        {
            get { return _isViability; }
            set
            {
                if (value != _isViability)
                {
                    _isViability = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public bool IsDebris
        {
            get { return !string.IsNullOrEmpty(_debriDisplay); }
        }

        public bool HasSubpopulations
        {
            get { return _hasSubpopulations; }
            set
            {
                if (value != _hasSubpopulations)
                {
                    _hasSubpopulations = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public string Subpopulation
        {
            get { return _subpopulation; }
            set
            {
                if(value != _subpopulation)
                {
                    this._subpopulation = value;
                    base.Cursor.Subpopulation = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public IEnumerable<string> AvailableSubPopulations
        {
            get { return _availableSubPopulations; }
            set
            {
                if(value != _availableSubPopulations)
                {
                    this._availableSubPopulations = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public string CountsPercentageA
        {
            get { return _countsPercentageA; }
            set
            {
                if (value != _countsPercentageA)
                {
                    this._countsPercentageA = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public bool HasSubpopulationA
        {
            get { return _hasSubpopulationA; }
            set
            {
                if (value != _hasSubpopulationA)
                {
                    _hasSubpopulationA = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public string CountsPerMlA
        {
            get { return _countsPerMlA; }
            set
            {
                if (value != _countsPerMlA)
                {
                    this._countsPerMlA = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public string CountsPercentageB
        {
            get { return _countsPercentageB; }
            set
            {
                if (value != _countsPercentageB)
                {
                    this._countsPercentageB = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public bool HasSubpopulationB
        {
            get { return _hasSubpopulationB; }
            set
            {
                if (value != _hasSubpopulationB)
                {
                    _hasSubpopulationB = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public string CountsPerMlB
        {
            get { return _countsPerMlB; }
            set
            {
                if (value != _countsPerMlB)
                {
                    this._countsPerMlB = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public string CountsPercentageC
        {
            get { return _countsPercentageC; }
            set
            {
                if (value != _countsPercentageC)
                {
                    this._countsPercentageC = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public bool HasSubpopulationC
        {
            get { return _hasSubpopulationC; }
            set
            {
                if (value != _hasSubpopulationC)
                {
                    _hasSubpopulationC = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public string CountsPerMlC
        {
            get { return _countsPerMlC; }
            set
            {
                if (value != _countsPerMlC)
                {
                    this._countsPerMlC = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public string CountsPercentageD
        {
            get { return _countsPercentageD; }
            set
            {
                if (value != _countsPercentageD)
                {
                    this._countsPercentageD = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public bool HasSubpopulationD
        {
            get { return _hasSubpopulationD; }
            set
            {
                if (value != _hasSubpopulationD)
                {
                    _hasSubpopulationD = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public string CountsPerMlD
        {
            get { return _countsPerMlD; }
            set
            {
                if (value != _countsPerMlD)
                {
                    this._countsPerMlD = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public string CountsPercentageE
        {
            get { return _countsPercentageE; }
            set
            {
                if (value != _countsPercentageE)
                {
                    this._countsPercentageE = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public bool HasSubpopulationE
        {
            get { return _hasSubpopulationE; }
            set
            {
                if (value != _hasSubpopulationE)
                {
                    _hasSubpopulationE = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public string CountsPerMlE
        {
            get { return _countsPerMlE; }
            set
            {
                if (value != _countsPerMlE)
                {
                    this._countsPerMlE = value;
                    NotifyOfPropertyChange();
                }
            }
        }
    }
}
