using OLS.Casy.Models;

namespace OLS.Casy.Ui.Core.Api
{
    public interface IAuditTrailViewModel
    {
        void LoadAuditTrailEntries(MeasureResult measureResult);
        void LoadAuditTrailEntries(MeasureSetup template);
    }
}
