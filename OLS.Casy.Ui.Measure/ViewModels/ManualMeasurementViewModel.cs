using OLS.Casy.Controller.Api;
using OLS.Casy.Core;
using OLS.Casy.Models;
using OLS.Casy.Models.Enums;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Core.Api;
using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace OLS.Casy.Ui.Measure.ViewModels
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(ManualMeasurementViewModel))]
    public class ManualMeasurementViewModel : ValidationViewModelBase, IPartImportsSatisfiedNotification
    {
        private readonly IMeasureController _measureController;
        private readonly ICalibrationController _calibrationController;
        private readonly IMeasureResultManager _measureResultManager;
        private MeasureSetup _template;
        private bool _ignoreRecalculateSampleVolume;

        [ImportingConstructor]
        public ManualMeasurementViewModel(
            IMeasureController measureController,
            ICalibrationController calibrationController,
            IMeasureResultManager measureResultManager)
        {
            _measureController = measureController;
            _calibrationController = calibrationController;
            _measureResultManager = measureResultManager;

            AvailableCapillarySizes = new SmartCollection<string>();
            KnownToDiameters = new SmartCollection<string>();
        }

        public SmartCollection<string> AvailableCapillarySizes { get; }

        public string CapillarySize
        {
            get => _template == null || _template.CapillarySize == 0 ? null : _template.CapillarySize.ToString();
            set
            {
                if (value == _template.CapillarySize.ToString()) return;

                _template.CapillarySize = !string.IsNullOrEmpty(value) ? int.Parse(value) : 60;

                if(_measureController.LastSelectedCapillary != _template.CapillarySize)
                {
                    _measureController.LastSelectedCapillary = _template.CapillarySize;
                }

                NotifyOfPropertyChange();
                SetDefaults();
            }
        }

        public SmartCollection<string> KnownToDiameters { get; }

        public string ToDiameter
        {
            get => _template == null || _template.ToDiameter == 0 ? null : _template.ToDiameter.ToString();
            set
            {
                if (value == null || value == _template.ToDiameter.ToString()) return;
                _template.ToDiameter = !string.IsNullOrEmpty(value) ? int.Parse(value) : 20;
                NotifyOfPropertyChange();
                CreateDefaultCursor();
            }
        }

        public double Repeats
        {
            get => _template?.Repeats ?? 0d;
            set
            {
                if (value == _template.Repeats) return;

                _template.Repeats = (int)value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange("CanMeasure");
            }
        }

        public Volumes Volume
        {
            get => _template?.Volume ?? Volumes.TwoHundred;
            set
            {
                if (value == _template.Volume) return;
                _template.Volume = value;
                NotifyOfPropertyChange();
            }
        }

        public bool IsMultiCursorMode
        {
            get => _template != null && _template.MeasureMode == MeasureModes.MultipleCursor;
            set
            {
                if (_template.MeasureMode == MeasureModes.MultipleCursor) return;
                _template.MeasureMode = MeasureModes.MultipleCursor;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange("IsViabilityMode");
                CreateDefaultCursor();
            }
        }

        public bool IsViabilityMode
        {
            get => _template != null && _template.MeasureMode == MeasureModes.Viability;
            set
            {
                if (_template.MeasureMode == MeasureModes.Viability) return;
                _template.MeasureMode = MeasureModes.Viability;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange("IsMultiCursorMode");
                CreateDefaultCursor();
            }
        }

        [RegularExpression("[0-9]+")]
        public double DilutionFactor
        {
            get => _template?.DilutionFactor ?? 0d;
            set
            {
                if (value == _template.DilutionFactor) return;
                _template.DilutionFactor = value;
                CalcDilutionSampleVolume();
                NotifyOfPropertyChange();
            }
        }

        [RegularExpression("^[0-9]+([.,][0-9]+)?$")]
        public double DilutionSampleVolume
        {
            get => _template?.DilutionSampleVolume ?? 0d;
            set
            {
                if (value == _template.DilutionSampleVolume) return;
                _template.DilutionSampleVolume = value;
                NotifyOfPropertyChange();
                _ignoreRecalculateSampleVolume = true;
                CalcDilutionFactor();
                _ignoreRecalculateSampleVolume = false;
                NotifyOfPropertyChange("DilutionFactor");
            }
        }

        [RegularExpression("[0-9]+")]
        public double DilutionCasyTonVolume
        {
            get => _template?.DilutionCasyTonVolume ?? 0d;
            set
            {
                if (value == _template.DilutionCasyTonVolume) return;
                _template.DilutionCasyTonVolume = value;
                NotifyOfPropertyChange();
                _ignoreRecalculateSampleVolume = true;
                CalcDilutionFactor();
                _ignoreRecalculateSampleVolume = false;
                NotifyOfPropertyChange("DilutionFactor");
            }
        }

        private void CalcDilutionFactor()
        {
            if (DilutionCasyTonVolume > 0d && DilutionSampleVolume > 0d)
            {
                DilutionFactor = (DilutionCasyTonVolume * 1000 + DilutionSampleVolume) / DilutionSampleVolume;
            }
        }

        private void CalcDilutionSampleVolume()
        {
            if (!_ignoreRecalculateSampleVolume && DilutionCasyTonVolume == 10d)
            {
                DilutionSampleVolume = (1000 * DilutionCasyTonVolume) / (DilutionFactor - 1);
            }
        }

        public void OnImportsSatisfied()
        {
            _measureController.SelectedTemplateChangedEvent += OnSelectedTemplateChanged;
            AvailableCapillarySizes.AddRange(_calibrationController.KnownCappillarySizes.Select(capillary => capillary.ToString()));
        }

        private void CreateDefaultCursor()
        {
            while (_template.Cursors.Any())
            {
                _template.RemoveCursorAt(0);
            }

            if (_template.MeasureMode != MeasureModes.Viability) return;
            var minLimit = _measureResultManager.GetMinMeasureLimit(_template.CapillarySize);
            _template.Cursors.Add(new Cursor
            {
                Name = "Cursor_DeadCells_Name",
                MinLimit = minLimit,
                MaxLimit = _template.ToDiameter / 2d - 0.01,
                MeasureSetup = _template,
                IsDeadCellsCursor = true
            });

            _template.Cursors.Add(new Cursor
            {
                Name = "Cursor_VitalCells_Name",
                MinLimit = _template.ToDiameter / 2d,
                MaxLimit = _template.ToDiameter,
                MeasureSetup = _template
            });
        }

        private void OnSelectedTemplateChanged(object sender, SelectedTemplateChangedEventArgs e)
        {
            if (_template != null)
            {
                _template.PropertyChanged -= OnTemplatePropertyChanged;
            }

            _template = _measureController.SelectedTemplate;
            

            if (_template != null)
            {
                _template.PropertyChanged += OnTemplatePropertyChanged;
            }

            NotifyOfPropertyChange("CapillarySize");
            FillKnownToDiameters();

            NotifyOfPropertyChange("ToDiameter");
            SetDefaults();
        }

        private void FillKnownToDiameters()
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate
            {
                KnownToDiameters.Clear();

                if (_template != null && _template.CapillarySize != 0)
                {
                    KnownToDiameters.AddRange(_calibrationController.GetDiametersByCappillarySize(_template.CapillarySize).Select(size => size.ToString()));
                }
            }, DispatcherPriority.ApplicationIdle);
        }

        private void OnTemplatePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "CapillarySize":
                {
                    FillKnownToDiameters();

                    if (!string.IsNullOrEmpty(ToDiameter))
                    {
                        if (!KnownToDiameters.Contains(ToDiameter))
                        {
                            ToDiameter = null;
                        }
                        else
                        {
                            NotifyOfPropertyChange("ToDiameter");
                        }
                    }
                    NotifyOfPropertyChange("CapillarySize");
                    break;
                }
                case "ToDiameter":
                    NotifyOfPropertyChange("ToDiameter");
                    break;
                default:
                    NotifyOfPropertyChange(e.PropertyName);
                    break;
            }
        }

        private void SetDefaults()
        {
            if (_template == null || _template.CapillarySize != 150 || !_template.IsManual ||
                _template.MeasureSetupId != -1) return;
            Volume = Volumes.FourHundred;
            Repeats = 3;
        }
    }
}
