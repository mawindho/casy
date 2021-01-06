using OLS.OmniBus.Server.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OLS.Casy.Com.OmniBus
{
    public interface IOmniBusDataService
    {
        Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances();
    }
}
