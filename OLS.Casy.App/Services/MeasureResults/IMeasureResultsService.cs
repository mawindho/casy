using System;
using OLS.Casy.App.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OLS.Casy.App.Services.MeasureResults
{
    public interface IMeasureResultsService
    {
        Task<IEnumerable<Experiment>> GetExperiments();
        Task<IEnumerable<Group>> GetGroups(string experiment);
        Task<IEnumerable<MeasureResult>> GetMeasureResults(string experiment, string group);
        IEnumerable<MeasureResult> SelectedMeasureResults { get; }
        Task AddSelectedMeasureResult(MeasureResult measureResult);
        Task AddSelectedMeasureResult(LastSelected lastSelected);
        Task RemoveSelectedMeasureResult(MeasureResult measureResult);
        Task<IEnumerable<MeasureResult>> GetOverlay();
        Task<Tuple<MeasureResult, IEnumerable<MeasureResult>>> GetMean();
        event EventHandler SelectedMeasureResultsChanged;
    }
}
