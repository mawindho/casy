using OLS.Casy.App.Models;
using System.Collections.Generic;

namespace OLS.Casy.App.Services.Detection
{
    public interface IDetectionService
    {
        void Initialize();
        IEnumerable<CasyModel> CasyModels { get; }
    }
}
