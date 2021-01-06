using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OLS.Casy.Core.Api
{
    public interface IActivationService
    {
        Task<bool> CheckActivation(Action<object> showMessageDialogDelegate, Action<object> showInputDialogDelegate, IProgress<string> splashProgress = null);
        bool IsModuleEnabled(string module);
    }
}
