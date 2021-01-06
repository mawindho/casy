using OLS.Casy.Models;
using System.Threading.Tasks;

namespace OLS.Casy.IO.Api
{
    public interface ICRFImportExportProvider
    {
        Task<MeasureResult> ImportAsync(string filePath, string color);
    }
}
