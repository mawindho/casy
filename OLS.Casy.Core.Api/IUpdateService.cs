using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OLS.Casy.Core.Api
{
    public interface IUpdateService
    {
        void CheckForOnlineUpdate();
        Task CheckForUpdates(bool confirmationRequired = true, IProgress<string> outerProgress = null, string currentVersionParam = null);
        void ExtractZipFile(string archiveFilenameIn, string password, string outFolder);
        Task OnUsbStickDetected(string usbPath);
    }
}
