using OLS.Casy.Models;
using System.Collections.Generic;

namespace OLS.Casy.Ui.Core.Api
{
    public enum DocumentType
    {
        SingleMeasurement,
        Overlay,
        Mean
    }

    public interface IDocumentSettingsManager
    {
        string DocumentLogoName { get; }
        void UpdateDocumentLogoName(string documentLogoName);
        List<DocumentSetting> SingleDocumentSettings { get; set; }
        List<DocumentSetting> OverlayDocumentSettings { get; set; }
        List<DocumentSetting> MeanDocumentSettings { get; set; }
        void LoadDocumentSettings(DocumentType documentType);
        void SaveDocumentSettings(DocumentType documentType);
        bool ShowLastWeeklyClean { get; set; }
    }
}
