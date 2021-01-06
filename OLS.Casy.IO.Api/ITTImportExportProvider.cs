using OLS.Casy.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OLS.Casy.IO.Api
{
    public interface ITTImportExportProvider
    {
        Task<IEnumerable<MeasureResult>> ImportAsync(string filePath, int lastColorIndex);

        Task<IEnumerable<MeasureResult>> ImportXlsxAsync(string filePath, int lastColorIndex);
    }
}
