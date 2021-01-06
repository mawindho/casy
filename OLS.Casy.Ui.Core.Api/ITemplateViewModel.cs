namespace OLS.Casy.Ui.Core.Api
{
    public interface ITemplateViewModel
    {
        int TemplateId { get; }
        bool IsSelected { get; set; }
        bool IsFavorite { get; set; }
    }
}
