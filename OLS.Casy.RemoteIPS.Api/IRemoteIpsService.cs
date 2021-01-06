using OLS.Casy.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OLS.Casy.RemoteIPS.Api
{
    public interface IRemoteIpsService
    {
        IEnumerable<string> GetWorkbookNames();
        Task PostMeasureResult(MeasureResult measureResult, string workbookName);
    }
}
