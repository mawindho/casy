using OLS.Casy.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OLS.Casy.IO.Api
{
    public interface IRawDataExportProvider
    {
        Task ExportMeasureResultsAsync(IEnumerable<MeasureResult> measureResults, string filePath);
    }
}
