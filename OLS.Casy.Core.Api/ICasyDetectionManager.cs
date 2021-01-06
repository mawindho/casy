using OLS.Casy.Models;
using System.Collections.Generic;

namespace OLS.Casy.Core.Api
{
    public interface ICasyDetectionManager
    {
        bool TryAddCasy(CasyModel casyModel);
        IEnumerable<CasyModel> CasyModels { get; }
    }
}
