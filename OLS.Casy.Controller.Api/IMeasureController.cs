using OLS.Casy.Core.Api;
using OLS.Casy.Core.Events;
using OLS.Casy.Models;
using OLS.Casy.Models.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OLS.Casy.Controller.Api
{
    public class SelectedTemplateChangedEventArgs : EventArgs
    {
        public SelectedTemplateChangedEventArgs(MeasureSetup template)
        {
            this.Template = template;
        }
        public MeasureSetup Template { get; private set; }
    }

    /// <summary>
    /// Interface for a controller handling measurement operations with the casy device
    /// </summary>
    public interface IMeasureController
    {
        /// <summary>
        /// Returns the currently active <see cref="MeasureSetup"/>
        /// </summary>
        MeasureSetup SelectedTemplate { get; set; }

        /// <summary>
        /// Event is raised when the currently active <see cref="MeasureSetup"/> has been changed 
        /// </summary>
        event EventHandler<SelectedTemplateChangedEventArgs> SelectedTemplateChangedEvent;

        /// <summary>
        /// Starts async a measurement
        /// </summary>
        /// <returns><see cref="ErrorResult"/> of the measurement operation</returns>
        Task<MeasureResult> Measure(MeasureResult measureResult, ShowProgressDialogWrapper showProgressWrapper = null, string workbook = null, int? customRepeats = null);
        Task<MeasureResultData> GetCachedMeasureResult(MeasureResult measureResult, IProgress<string> progress);
        Task<ButtonResult> HandleSoftErrors(Progress<string> progress, IEnumerable<ErrorDetails> softErrors, MeasureResult measureResult, LogCategory logCategory);

        /// <summary>
        /// Starts a clean operation with the passed count.
        /// </summary>
        /// <param name="cleanCount">Clean count</param>
        /// <returns><see cref="ErrorResult"/> representing the operations result</returns>
        ErrorResult Clean(int cleanCount = 1);

        ErrorResult CleanWaste();

        ErrorResult CleanCapillary();

        bool IsValidTemplate(MeasureSetup template);

        string ConnectedSerialPort { get; }

        int LastColorIndex { get; set; }

        int LastSelectedCapillary { get; set; }
    }
}
