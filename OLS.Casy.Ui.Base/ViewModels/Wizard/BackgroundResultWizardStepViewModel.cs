using DevExpress.Mvvm;
using OLS.Casy.Models;
using OLS.Casy.Ui.Base.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace OLS.Casy.Ui.Base.ViewModels.Wizard
{
    public class BackgroundResultWizardStepViewModel : WizardStepViewModelBase
    {
        private string _totalCounts;
        private string _totalCountsState;

        private string _header;
        private string _text;
        private ChartDataItemModel<double, double>[] _measureResultData;
        private bool _isPrintButtonVisible = true;

        public int GotoNext { get; set; }
        public int GotoCancel { get; set; }

        public string Header
        {
            get { return _header; }
            set
            {
                if (value != _header)
                {
                    _header = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public string Text
        {
            get { return _text; }
            set
            {
                if (value != _text)
                {
                    _text = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public string TotalCounts
        {
            get { return _totalCounts; }
            set
            {
                if (value != _totalCounts)
                {
                    _totalCounts = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public string TotalCountsState
        {
            get { return _totalCountsState; }
            set
            {
                if (value != _totalCountsState)
                {
                    _totalCountsState = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public IEnumerable<ChartDataItemModel<double, double>> MeasureResultData
        {
            get
            {
                if (_measureResultData == null)
                {
                    return new ChartDataItemModel<double, double>[0];
                }

                var arrayLength = _measureResultData.Length;

                ChartDataItemModel<double, double>[] result = new ChartDataItemModel<double, double>[arrayLength];

                Array.Copy(_measureResultData, 0, result, 0, arrayLength);

                return result;
            }
        }

        public MeasureResult MeasureResult { get; set; }

        public void SetChartData(double[] chartDataSet, double[] smoothedDiameters)
        {
            _measureResultData = new ChartDataItemModel<double, double>[chartDataSet.Length];

            for (int i = 0; i < chartDataSet.Length; i++)
            {
                this._measureResultData[i] = new ChartDataItemModel<double, double>(string.Empty, smoothedDiameters[i], chartDataSet[i], "#FF009FE3");
            }

            Application.Current.Dispatcher.BeginInvoke((Action)delegate ()
            {
                NotifyOfPropertyChange("MeasureResultData");
            }, DispatcherPriority.ApplicationIdle);
        }

        public Action NextButtonPressedAction { get; set; }

        public async override Task<bool> OnNextButtonPressed()
        {
            if (NextButtonPressedAction != null)
            {
                await Task.Factory.StartNew(NextButtonPressedAction);
            }
            await base.OnNextButtonPressed();
            return true;
        }

        public Action CancelButtonPressedAction { get; set; }

        public async override Task<bool> OnCancelButtonPressed()
        {
            if (CancelButtonPressedAction != null)
            {
                await Task.Factory.StartNew(CancelButtonPressedAction);
                return true;
            }
            return await base.OnCancelButtonPressed();
        }

        public bool IsPrintButtonVisible {
            get { return _isPrintButtonVisible; }
            set
            {
                _isPrintButtonVisible = value;
                NotifyOfPropertyChange();
            }
        }

        public Action PrintButtonPressedAction { get; set; }

        public ICommand PrintCommand
        {
            get { return new OmniDelegateCommand(OnPrint); }
        }

        private async void OnPrint()
        {
            if (PrintButtonPressedAction != null)
            {
                await Task.Factory.StartNew(PrintButtonPressedAction);
            }
        }
    }
}
