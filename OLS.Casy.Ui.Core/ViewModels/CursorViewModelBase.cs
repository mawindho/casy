using DevExpress.Mvvm;
using OLS.Casy.Core;
using OLS.Casy.Models;
using OLS.Casy.Ui.Base;
using OLS.Casy.Ui.Core.Api;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Timers;
using System.Windows;

namespace OLS.Casy.Ui.Core.ViewModels
{
    public abstract class CursorViewModelBase : ValidationViewModelBase, IDisposable
    {
        private readonly IUIProjectManager _uiProject;
        private readonly IMeasureResultManager _measureResultManager;

        private Cursor _cursor;

        //private double _oldMinLimit;
        //private double _oldMaxLimit;
        private bool _isReadOnly;

        //private double _minLimitSmoothed;
        //private double _maxLimitSmoothed;

        public CursorViewModelBase(IUIProjectManager uiProject, IMeasureResultManager measureResultManager)
        {
            this._uiProject = uiProject;
            this._measureResultManager = measureResultManager;
        }

        public Cursor Cursor
        {
            get { return this._cursor; }
            set
            {
                if (this._cursor != null)
                {
                    this._cursor.PropertyChanged -= OnCursorChanged;
                }
                this._cursor = value;

                if (this._cursor != null)
                {
                    this._cursor.PropertyChanged += OnCursorChanged;
                }

                NotifyOfPropertyChange("Name");
                NotifyOfPropertyChange("MinLimit");
                NotifyOfPropertyChange("MaxLimit");
                NotifyOfPropertyChange("Color");
                NotifyOfPropertyChange("IsVisible");

                //_minLimitSmoothed = _cursor.MeasureSetup.SmoothedDiameters[_cursor.MinLimit];
                //NotifyOfPropertyChange("MinLimitSmoothed");

                //_maxLimitSmoothed = _cursor.MeasureSetup.SmoothedDiameters[_cursor.MaxLimit];
                //NotifyOfPropertyChange("MaxLimitSmoothed");

                _cursor.OldMinLimit = _cursor.MinLimit;
                _cursor.OldMaxLimit = _cursor.MaxLimit;

                this.OnCursorChanged();
            }
        }

        protected virtual void OnCursorChanged()
        {
        }

        //[Required]
        //public double MinLimitSmoothed
        //{
        //    get { return _minLimitSmoothed; }
        //    set
        //    {
        //        if (value != _minLimitSmoothed)
        //        {
        //            //if (value < _maxLimitSmoothed)
        //            //{
        //                var lowerCursor = _cursor.MeasureSetup.Cursors.Where(c => c != _cursor && c.MaxLimit < _cursor.MaxLimit);
        //                int nextCursorMax = int.MinValue;
        //                if (lowerCursor.Any())
        //                {
        //                    nextCursorMax = lowerCursor.Max(c => c.MaxLimit);
        //                }

        //                var channelValue = Calculations.CalcChannel(_cursor.MeasureSetup.FromDiameter, _cursor.MeasureSetup.ToDiameter, value);

        //            if (channelValue <= nextCursorMax)
        //            {
        //                var cursor = lowerCursor.FirstOrDefault(c => c.MaxLimit == nextCursorMax);

        //                if (cursor != null)
        //                {
        //                    cursor.MaxLimit = channelValue - 1;
        //                }
        //            }
        //                    _minLimitSmoothed = value;

        //                    if (channelValue != _cursor.MinLimit)
        //                    {
        //                        MinLimit = channelValue;
        //                    }
        //                //}
        //            //}
        //            NotifyOfPropertyChange();
        //        }
        //    }
        //}

        [Required]
        public double MinLimit
        {
            get => _cursor?.MinLimit ?? 0d;
            set
            {
                if(_cursor != null && value != _cursor.MinLimit)
                {
                    var lowerCursor = _cursor.MeasureSetup.Cursors.Where(c => c != null && c != _cursor && c.MaxLimit < _cursor.MinLimit);
                    var nextCursorMax = double.MinValue;
                    if (lowerCursor.Any())
                    {
                        nextCursorMax = lowerCursor.Max(c => c.MaxLimit);
                    }

                    if (value > nextCursorMax)
                    {
                        if (_cursor.MeasureSetup == null) return;

                        _measureResultManager.StopRangeModificationTimer(_cursor.MeasureSetup);

                        if (_cursor.MeasureSetup.MeasureMode == Casy.Models.Enums.MeasureModes.Viability)
                        {
                            var otherCursor = _cursor.MeasureSetup.Cursors?.FirstOrDefault(c => c != _cursor);
                            if (otherCursor != null && otherCursor.MaxLimit <= _cursor.MinLimit)
                            {
                                otherCursor.MaxLimit = value - 0.01;
                            }
                        }
                        else
                        {
                            foreach (var otherCursor in lowerCursor)
                            {
                                if (otherCursor.MinLimit > value)
                                {
                                    otherCursor.MaxLimit = value + 0.01;
                                    otherCursor.MinLimit = value + 0.02;
                                }
                                else if (otherCursor.MaxLimit > value)
                                {
                                    otherCursor.MaxLimit = value - 0.01;
                                }
                            }
                        }

                        _cursor.MinLimit = value;

                        _measureResultManager.StartRangeModificationTimer(this._cursor.MeasureSetup);
                    }
                }
            }
        }

        //[Required]
        //public double MaxLimitSmoothed
        //{
        //    get { return _maxLimitSmoothed; }
        //    set
        //    {
        //        if(value != _maxLimitSmoothed)
        //        {
        //            //if (value > _minLimitSmoothed)
        //            //{
        //                var higherCursor = _cursor.MeasureSetup.Cursors.Where(c => c != _cursor && c.MinLimit > _cursor.MaxLimit);
        //                int nextCursorMin = int.MaxValue;
        //                if (higherCursor.Any())
        //                {
        //                    nextCursorMin = higherCursor.Min(c => c.MinLimit);
        //                }

        //                var channelValue = Calculations.CalcChannel(_cursor.MeasureSetup.FromDiameter, _cursor.MeasureSetup.ToDiameter, value);

        //            //if (_cursor.MeasureSetup.MeasureMode == Casy.Models.Enums.MeasureModes.MultipleCursor)
        //            //{ 
        //            //if (channelValue < nextCursorMin)
        //            //{
        //            if (channelValue >= nextCursorMin)
        //            {
        //                var cursor = higherCursor.FirstOrDefault(c => c.MinLimit == nextCursorMin);

        //                if (cursor != null)
        //                {
        //                    cursor.MinLimit = channelValue + 1;
        //                }
        //            }

        //            this._maxLimitSmoothed = value;

        //            if (channelValue != _cursor.MaxLimit)
        //            {
        //                MaxLimit = channelValue;
        //            }


        //                //}
        //                //}
        //                NotifyOfPropertyChange();
        //        }
        //    }
        //}

        [Required]
        public double MaxLimit
        {
            get => _cursor?.MaxLimit ?? 0d;
            set
            {
                if (_cursor != null && value != _cursor.MaxLimit)
                {
                    var higherCursor = _cursor.MeasureSetup.Cursors.Where(c => c != null && c != _cursor && c.MinLimit > _cursor.MaxLimit);
                    double nextCursorMin = double.MaxValue;
                    if (higherCursor.Any())
                    {
                        nextCursorMin = higherCursor.Min(c => c.MinLimit);
                    }

                    if (value < nextCursorMin)
                    {
                        if (_cursor.MeasureSetup == null) return;

                        _measureResultManager.StopRangeModificationTimer(_cursor.MeasureSetup);

                        if (_cursor.MeasureSetup.MeasureMode == Casy.Models.Enums.MeasureModes.Viability)
                        {
                            //if (cursor != null)
                            //{
                                //cursor.MinLimit = value + 0.01;
                            //}

                            var otherCursor = _cursor.MeasureSetup.Cursors?.FirstOrDefault(c => c != _cursor);
                            if (otherCursor != null && otherCursor.MinLimit >= _cursor.MaxLimit)
                            {
                                otherCursor.MinLimit = value + 0.01;
                            }
                        }
                        else
                        {
                            foreach(var otherCursor in higherCursor)
                            {
                                if (otherCursor.MaxLimit < value)
                                {
                                    otherCursor.MaxLimit = value + 0.01;
                                    otherCursor.MinLimit = value + 0.02;
                                }
                                else if (otherCursor.MinLimit < value)
                                {
                                    otherCursor.MinLimit = value + 0.01; 
                                }
                            }
                        }

                        _cursor.MaxLimit = value;

                        _measureResultManager.StartRangeModificationTimer(this._cursor.MeasureSetup);
                    }
                }
            }
        }

        [Required]
        public virtual string Name
        {
            get { return _cursor == null ? string.Empty : _cursor.Name; }
            set
            {
                if (value != _cursor.Name)
                {
                    this._uiProject.SendUIEdit(_cursor, "Name", value);
                }
            }
        }

        public string Color
        {
            get { return _cursor == null ? string.Empty : _cursor.Color; }
            set
            {
                if (value != _cursor.Color)
                {
                    this._uiProject.SendUIEdit(_cursor, "Color", value);
                }
            }
        }

        public bool IsReadOnly
        {
            get { return _isReadOnly; }
            set
            {
                if (value != _isReadOnly)
                {
                    _isReadOnly = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        public bool IsVisible
        {
            get { return _cursor == null ? false : _cursor.IsVisible; }
        }

        public int ToDiameter
        {
            get { return this._cursor.MeasureSetup.ToDiameter; }
        }

        protected virtual void OnCursorChanged(object sender, PropertyChangedEventArgs e)
        {
            NotifyOfPropertyChange(e.PropertyName);

            switch (e.PropertyName)
            {
                case "MinLimit":
                    //this._cursor.OldMinLimit = this._cursor.MinLimit;
                    //var smoothedMinLimit = _cursor.MeasureSetup.SmoothedDiameters[_cursor.MinLimit];
                    //if (smoothedMinLimit != this._minLimitSmoothed)
                    //{
                    //    _minLimitSmoothed = smoothedMinLimit;
                    //    NotifyOfPropertyChange("MinLimitSmoothed");
                    //}
                    break;
                case "MaxLimit":
                    //this._cursor.OldMaxLimit = this._cursor.MaxLimit;
                    //var smoothedMaxLimit = _cursor.MeasureSetup.SmoothedDiameters[_cursor.MaxLimit];
                    //if (smoothedMaxLimit != this._maxLimitSmoothed)
                    //{
                    //    _maxLimitSmoothed = _cursor.MeasureSetup.SmoothedDiameters[_cursor.MaxLimit];
                    //    NotifyOfPropertyChange("MaxLimitSmoothed");
                    //}
                    break;
                //case "Name":
                //    NotifyOfPropertyChange("Name");
                //    break;
            }
        }

        public OmniDelegateCommand<EventInformation<RoutedEventArgs>> LostFocusCommand
        {
            get { return new OmniDelegateCommand<EventInformation<RoutedEventArgs>>(OnLostFocus); }
        }

        private void OnLostFocus(EventInformation<RoutedEventArgs> obj)
        {
            //var smoothedMinLimit = _cursor.MeasureSetup.SmoothedDiameters[_cursor.MinLimit];
            //if (smoothedMinLimit != this._minLimitSmoothed)
            //{
            //    _minLimitSmoothed = smoothedMinLimit;
            //    NotifyOfPropertyChange("MinLimitSmoothed");
            //}

            //var smoothedMaxLimit = _cursor.MeasureSetup.SmoothedDiameters[_cursor.MaxLimit];
            //if (smoothedMaxLimit != this._maxLimitSmoothed)
            //{
            //    _maxLimitSmoothed = _cursor.MeasureSetup.SmoothedDiameters[_cursor.MaxLimit];
            //    NotifyOfPropertyChange("MaxLimitSmoothed");
            //}
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    //if(_updateTimer != null)
                    //{
                    //    this._updateTimer.Elapsed -= OnTimerElapsed;
                    //    this._updateTimer.Dispose();
                    //}

                    //if(_minTimer != null)
                    //{
                        //this._minTimer.Dispose();
                    //}
                }

                disposedValue = true;
            }
            base.Dispose(disposing);
        }
        #endregion
    }
}
