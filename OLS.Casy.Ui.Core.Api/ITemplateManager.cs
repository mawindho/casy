using OLS.Casy.Models;
using System.Threading.Tasks;

namespace OLS.Casy.Ui.Core.Api
{
    public interface ITemplateManager
    {
        void DeleteTemplate(MeasureSetup template);
        Task<bool> SaveTemplate(MeasureSetup template);
        void CloneSetup(MeasureSetup measureSetup, ref MeasureSetup newSetup);
    }
}
