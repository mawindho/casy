using OLS.Casy.Models.Enums;
using OLS.Casy.Ui.Base.Controls;
using OLS.Casy.Ui.Core.ViewModels;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;

namespace OLS.Casy.Ui.Core.Controls
{
    public class MoveRangeThumb : MoveThumb
    {
        public MoveRangeThumb()
        {
            DragDelta += new DragDeltaEventHandler(this.MoveThumb_DragDelta);
            this.AddHandler(System.Windows.Controls.Control.PreviewMouseDoubleClickEvent, new MouseButtonEventHandler((target, args) =>
           {
               if(this.DoubleClickCommand != null)
               {
                   this.DoubleClickCommand.Execute(this);
               }
                args.Handled = true;
           }));
        }

        public static readonly DependencyProperty DoubleClickCommandProperty =
            DependencyProperty.Register("DoubleClickCommand", typeof(ICommand), typeof(MoveRangeThumb), new UIPropertyMetadata(null));


        public ICommand DoubleClickCommand
        {
            get { return (ICommand)GetValue(DoubleClickCommandProperty); }
            set { SetValue(DoubleClickCommandProperty, value); }
        }

        private void MoveThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            MoveThumb thumb = e.Source as MoveThumb;

            if (thumb != null)
            {
                RangeMinModificationHandleViewModel minViewModel = this.DataContext as RangeMinModificationHandleViewModel;
                if (minViewModel != null && minViewModel.CanModifyRange && minViewModel.IsValidHorizontalChange != null)
                {
                    if (minViewModel.IsValidHorizontalChange != null && minViewModel.IsValidHorizontalChange(e.HorizontalChange, true))
                    {
                        minViewModel.Width = minViewModel.Width + e.HorizontalChange;
                    }
                    return;
                }

                RangeMaxModificationHandleViewModel maxViewModel = this.DataContext as RangeMaxModificationHandleViewModel;
                if (maxViewModel != null && maxViewModel.CanModifyRange && maxViewModel.IsValidHorizontalChange != null)
                {
                    if (maxViewModel.IsValidHorizontalChange != null && maxViewModel.IsValidHorizontalChange(e.HorizontalChange, false))
                    {
                        maxViewModel.PositionLeft = maxViewModel.PositionLeft + e.HorizontalChange;
                        maxViewModel.Width = maxViewModel.Width - e.HorizontalChange;
                    }
                    return;
                }

                RangeBiModificationHandleViewModel biViewModel = this.DataContext as RangeBiModificationHandleViewModel;
                if (biViewModel != null && biViewModel.CanModifyRange)
                {
                    if (biViewModel.Parent != null &&
                        biViewModel.Parent.Cursor != null &&
                        biViewModel.Parent.Cursor.MeasureSetup != null &&
                        biViewModel.Parent.Cursor.MeasureSetup.MeasureMode == MeasureModes.Viability)
                    {
                        if (biViewModel.IsValidHorizontalChange != null && biViewModel.IsValidHorizontalChange(e.HorizontalChange, null))
                        {
                            biViewModel.PositionLeft = biViewModel.PositionLeft + e.HorizontalChange;
                            biViewModel.OnWidthChanged(biViewModel.Width);
                        }
                    }
                    else
                    {
                        switch (thumb.Name)
                        {
                            case "MinThumb":
                                if (biViewModel.IsValidHorizontalChange != null && biViewModel.IsValidHorizontalChange(e.HorizontalChange, true))
                                {
                                    biViewModel.Width = biViewModel.Width + e.HorizontalChange; ;
                                }
                                break;
                            case "MaxThumb":
                                if (biViewModel.IsValidHorizontalChange != null && biViewModel.IsValidHorizontalChange(e.HorizontalChange, false))
                                {
                                    biViewModel.PositionLeft = biViewModel.PositionLeft + e.HorizontalChange;
                                    biViewModel.Width = biViewModel.Width - e.HorizontalChange;
                                }
                                break;
                        }
                    }
                }
            }
        }
    }
}
