using OLS.Casy.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace OLS.Casy.Ui.Core.Api
{
    public interface IMeasureResultContainerViewModel : IDisposable, INotifyPropertyChanged
    {
        void AddMeasureResults(MeasureResult[] measureResults);
        void RemoveMeasureResult(MeasureResult measureResult);
        void ClearMeasureResults();
        bool IsSaveVisible { get; set; }
        MeasureSetup MeasureSetup { get; set; }
        bool IsExpandViewCollapsed { get; set; }
        bool IsButtonMenuCollapsed { get; set; }
        MeasureResult SingleResult { get; }
        int DisplayOrder { get; set; }
        bool IsVisible { get; set; }
        bool IsApplyToParentsAvailable { get; set; }
        bool CanApplyToParents { get; set; }
        bool IsShowParentsAvailable { get; set; }
        Task UpdateRowHeightAsync();
        IEnumerable<MeasureResult> MeasureResults { get; }
        void ForceUpdate();
        bool IsOverlayMode { get; set; }
        void CaptureImage(bool isPrintAll = true);
        object CurrentDocument { get; set; }
    }
}
