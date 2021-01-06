using System.Threading.Tasks;

namespace OLS.Casy.App.Services.Dialog
{
    public interface IDialogService
    {
        Task ShowAlertAsync(string message, string title, string buttonLabel);
    }
}
