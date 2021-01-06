using OLS.Casy.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OLS.Casy.IO.Api
{
    public interface IBinaryImportExportProvider
    {
        Task<IEnumerable<MeasureResult>> ImportMeasureResultsAsync(string filePath);
        Task ExportMeasureResultsAsync(IEnumerable<MeasureResult> measureResults, string filePath);
        Task<MeasureSetup> ImportMeasureSetupAsync(string filePath);
        Task ExportMeasureSetupAsync(MeasureSetup measureSetup, string filePath);
    }
}
