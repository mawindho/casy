using OLS.Casy.Models;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Core.Api;
using System.Windows;
using System.Windows.Media;

namespace OLS.Casy.Ui.Core.Models
{
    public class ChartColorModel : ViewModelBase 
    {
        private readonly IUIProjectManager _uiProject;
        private readonly MeasureResult _measureResult;

        private readonly bool _isTransparent;
        private int _chartThickness;

        public ChartColorModel(IUIProjectManager uiProject, MeasureResult measureResult, bool isTransparent, int chartThickness = 3)
        {
            _measureResult = measureResult;
            _uiProject = uiProject;
            _chartThickness = chartThickness;

            _isTransparent = isTransparent;
        }

        public string ChartName => _measureResult.Name;

        public string ChartColor
        {
            get
            {
                var color = string.IsNullOrEmpty(_measureResult.Color) ? ((SolidColorBrush)Application.Current.Resources["ChartColor1"]).Color.ToString() : _measureResult.Color;

                if (this._isTransparent)
                {
                    return color.Replace("#FF", "#44");
                }
                return color;
            }
            set
            {
                if (value != this._measureResult.Color && !_isTransparent)
                {
                    this._uiProject.SendUIEdit(this._measureResult, "Color", value);
                    NotifyOfPropertyChange();
                }
            }
        }

        public int ChartThickness
        {
            get { return _chartThickness; }
            set
            {
                if(_chartThickness != value)
                {
                    this._chartThickness = value;
                    NotifyOfPropertyChange();
                }
            }
        }
    }
}
