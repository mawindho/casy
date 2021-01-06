using OLS.Casy.IO.Api;
using OLS.Casy.Models;
using OLS.Casy.Ui.Core.Api;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace OLS.Casy.Ui.Core.Services
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IDocumentSettingsManager))]
    public class DocumentSettingsManager : IDocumentSettingsManager, IPartImportsSatisfiedNotification
    {
        private readonly IDatabaseStorageService _databaseStorageService;
        private bool _showLastWeeklyClean;

        [ImportingConstructor]
        public DocumentSettingsManager(IDatabaseStorageService databaseStorageService)
        {
            _databaseStorageService = databaseStorageService;
        }

        public List<DocumentSetting> SingleDocumentSettings { get; set; }
        public List<DocumentSetting> OverlayDocumentSettings { get; set; }
        public List<DocumentSetting> MeanDocumentSettings { get; set; }
        public string DocumentLogoName { get; private set; }
        public bool ShowLastWeeklyClean
        {
            get => _showLastWeeklyClean;
            set
            {
                if (value != _showLastWeeklyClean)
                {
                    _showLastWeeklyClean = value;
                    _databaseStorageService.SaveSetting("ShowLastWeeklyClean", _showLastWeeklyClean ? "true" : "false");
                }
            }
        }

        public void UpdateDocumentLogoName(string documentLogoName)
        {
            DocumentLogoName = documentLogoName;
            _databaseStorageService.SaveSetting("DocumentLogoName", DocumentLogoName);
        }

        public void LoadDocumentSettings(DocumentType documentType)
        {
            switch (documentType)
            {
                case DocumentType.SingleMeasurement:
                    var setting = _databaseStorageService.GetSettings()["SingleDocumentSettings"];
                    SingleDocumentSettings = DeserializeDocumentSettings(setting.BlobValue);
                    break;
                case DocumentType.Overlay:
                    setting = _databaseStorageService.GetSettings()["OverlayDocumentSettings"];
                    OverlayDocumentSettings = DeserializeDocumentSettings(setting.BlobValue);
                    break;
                case DocumentType.Mean:
                    setting = _databaseStorageService.GetSettings()["MeanDocumentSettings"];
                    MeanDocumentSettings = DeserializeDocumentSettings(setting.BlobValue);
                    break;
            }
        }

        public void SaveDocumentSettings(DocumentType documentType)
        {
            switch(documentType)
            {
                case DocumentType.SingleMeasurement:
                    _databaseStorageService.SaveSetting("SingleDocumentSettings", SerializeDocumentSettings(SingleDocumentSettings));
                    break;
                case DocumentType.Overlay:
                    _databaseStorageService.SaveSetting("OverlayDocumentSettings", SerializeDocumentSettings(OverlayDocumentSettings));
                    break;
                case DocumentType.Mean:
                    _databaseStorageService.SaveSetting("MeanDocumentSettings", SerializeDocumentSettings(MeanDocumentSettings));
                    break;
            }
        }

        public void OnImportsSatisfied()
        {
            var settings = _databaseStorageService.GetSettings();

            if (!settings.TryGetValue("SingleDocumentSettings", out _))
            {
                GenerateDefaultSettings(DocumentType.SingleMeasurement);
            }
            LoadDocumentSettings(DocumentType.SingleMeasurement);

            if (!settings.TryGetValue("OverlayDocumentSettings", out _))
            {
                GenerateDefaultSettings(DocumentType.Overlay);
            }
            LoadDocumentSettings(DocumentType.Overlay);

            if (!settings.TryGetValue("MeanDocumentSettings", out _))
            {
                GenerateDefaultSettings(DocumentType.Mean);
            }
            LoadDocumentSettings(DocumentType.Mean);

            if (!settings.TryGetValue("DocumentLogoName", out var documentLogoSetting))
            {
                UpdateDocumentLogoName("OLS_Logo.png");
                _databaseStorageService.SaveSetting("DocumentLogoName", DocumentLogoName);
            }
            else
            {
                DocumentLogoName = documentLogoSetting.Value;
            }

            if (!settings.TryGetValue("ShowLastWeeklyClean", out var showLastWeeklyCleanSetting))
            {
                _databaseStorageService.SaveSetting("ShowLastWeeklyClean", "false");
            }
            else
            {
                ShowLastWeeklyClean = showLastWeeklyCleanSetting.Value == "true";
            }
        }

        private static byte[] SerializeDocumentSettings(List<DocumentSetting> documentSettings)
        {
            using (var memoryStream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(memoryStream, documentSettings);

                memoryStream.Seek(0, SeekOrigin.Begin);

                var bytes = new byte[memoryStream.Length];
                memoryStream.Read(bytes, 0, (int)memoryStream.Length);

                return bytes;
            }
        }

        private static List<DocumentSetting> DeserializeDocumentSettings(byte[] data)
        {
            using (var memoryStream = new MemoryStream(data))
            {
                var formatter = new BinaryFormatter();
                var result = formatter.Deserialize(memoryStream) as List<DocumentSetting>;
                return result;
            }
        }

        private void GenerateDefaultSettings(DocumentType documentType)
        {
            switch (documentType)
            {
                case DocumentType.SingleMeasurement:
                    var documentSettings = new List<DocumentSetting>();
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.SampleVolume,
                        Name = "Sample Volume",
                        DisplayOrder = 0,
                        IsSelected = true,
                        Category = DocumentSettingCategory.Parameter
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.DilutionFactor,
                        Name = "Dilution Factor",
                        DisplayOrder = 1,
                        IsSelected = true,
                        Category = DocumentSettingCategory.Parameter
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.Capillary,
                        Name = "Capillary",
                        DisplayOrder = 2,
                        IsSelected = true,
                        Category = DocumentSettingCategory.Parameter
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.Smoothing,
                        Name = "Smoothing",
                        DisplayOrder = 3,
                        IsSelected = true,
                        Category = DocumentSettingCategory.Parameter
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.AggrCorrection,
                        Name = "Aggr. Correction",
                        DisplayOrder = 4,
                        IsSelected = true,
                        Category = DocumentSettingCategory.Parameter
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.SizeScale,
                        Name = "Size Scale",
                        DisplayOrder = 5,
                        IsSelected = true,
                        Category = DocumentSettingCategory.Parameter
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.Ranges,
                        Name = "Ranges",
                        DisplayOrder = 6,
                        IsSelected = true,
                        Category = DocumentSettingCategory.Parameter
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.Concentration,
                        Name = "Concentration",
                        DisplayOrder = 0,
                        IsSelected = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.Counts,
                        Name = "Counts",
                        DisplayOrder = 1,
                        IsSelected = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.ViableCellsMl,
                        Name = "Viable Cells/ml",
                        DisplayOrder = 2,
                        IsSelected = true,
                        IsViability = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.DeadCellsMl,
                        Name = "Dead Cells/ml",
                        DisplayOrder = 3,
                        IsSelected = true,
                        IsViability = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.TotalCellsMl,
                        Name = "Total Cells/ml",
                        DisplayOrder = 4,
                        IsSelected = true,
                        IsViability = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.CountsMl,
                        Name = "Counts/ml",
                        DisplayOrder = 5,
                        IsSelected = true,
                        IsFreeRanges = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.TotalCountsMl,
                        Name = "Total Counts/ml",
                        DisplayOrder = 6,
                        IsSelected = true,
                        IsFreeRanges = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.Viability,
                        Name = "% Viability",
                        DisplayOrder = 7,
                        IsSelected = true,
                        IsViability = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.ViableAggrFactor,
                        Name = "Aggr. Factor Viable Cells",
                        DisplayOrder = 8,
                        IsSelected = true,
                        IsViability = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.DebrisMl,
                        Name = "Debris / ml",
                        DisplayOrder = 8,
                        IsSelected = true,
                        IsViability = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.CellsPercentage,
                        Name = "% Cells",
                        DisplayOrder = 9,
                        IsSelected = true,
                        IsFreeRanges = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.AggrFactor,
                        Name = "Aggr. Factor",
                        DisplayOrder = 10,
                        IsSelected = true,
                        IsFreeRanges = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.CountsMlAboveRange,
                        Name = "Counts/ml > Max Range",
                        DisplayOrder = 11,
                        IsSelected = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.VolumeMlViable,
                        Name = "Volume/ml Viable Cells",
                        DisplayOrder = 12,
                        IsSelected = true,
                        IsViability = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.MeanDiameterViable,
                        Name = "Mean Diameter Viable Cells",
                        DisplayOrder = 13,
                        IsSelected = true,
                        IsViability = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.PeakDiameterViable,
                        Name = "Peak Diameter Viable Cells",
                        DisplayOrder = 13,
                        IsSelected = true,
                        IsViability = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.MeanVolumeViable,
                        Name = "Mean Volume Viable Cells",
                        DisplayOrder = 14,
                        IsSelected = true,
                        IsViability = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.PeakVolumeViable,
                        Name = "Peak Volume Viable Cells",
                        DisplayOrder = 15,
                        IsSelected = true,
                        IsViability = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.VolumeMl,
                        Name = "Volume/ml",
                        DisplayOrder = 16,
                        IsSelected = true,
                        IsFreeRanges = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.MeanDiameter,
                        Name = "Mean Diameter",
                        DisplayOrder = 17,
                        IsSelected = true,
                        IsFreeRanges = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.PeakDiameter,
                        Name = "Peak Diameter",
                        DisplayOrder = 18,
                        IsSelected = true,
                        IsFreeRanges = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.MeanVolume,
                        Name = "Mean Volume",
                        DisplayOrder = 19,
                        IsSelected = true,
                        IsFreeRanges = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.PeakVolume,
                        Name = "Peak Volume",
                        DisplayOrder = 20,
                        IsSelected = true,
                        IsFreeRanges = true,
                        Category = DocumentSettingCategory.Result
                    });

                    var blobData = SerializeDocumentSettings(documentSettings);
                    _databaseStorageService.SaveSetting("SingleDocumentSettings", blobData);
                    break;
                case DocumentType.Overlay:
                    documentSettings = new List<DocumentSetting>();
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.SampleVolume,
                        Name = "Sample Volume",
                        DisplayOrder = 0,
                        IsSelected = true,
                        Category = DocumentSettingCategory.Parameter
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.DilutionFactor,
                        Name = "Dilution Factor",
                        DisplayOrder = 1,
                        IsSelected = true,
                        Category = DocumentSettingCategory.Parameter
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.Capillary,
                        Name = "Capillary",
                        DisplayOrder = 2,
                        IsSelected = true,
                        Category = DocumentSettingCategory.Parameter
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.Smoothing,
                        Name = "Smoothing",
                        DisplayOrder = 3,
                        IsSelected = true,
                        Category = DocumentSettingCategory.Parameter
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.AggrCorrection,
                        Name = "Aggr. Correction",
                        DisplayOrder = 4,
                        IsSelected = true,
                        Category = DocumentSettingCategory.Parameter
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.SizeScale,
                        Name = "Size Scale",
                        DisplayOrder = 5,
                        IsSelected = true,
                        Category = DocumentSettingCategory.Parameter
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.Ranges,
                        Name = "Ranges",
                        DisplayOrder = 6,
                        IsSelected = true,
                        Category = DocumentSettingCategory.Parameter
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.Concentration,
                        Name = "Concentration",
                        DisplayOrder = 0,
                        IsSelected = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.Counts,
                        Name = "Counts",
                        DisplayOrder = 1,
                        IsSelected = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.ViableCellsMl,
                        Name = "Viable Cells/ml",
                        DisplayOrder = 2,
                        IsSelected = true,
                        IsViability = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.DeadCellsMl,
                        Name = "Dead Cells/ml",
                        DisplayOrder = 3,
                        IsSelected = true,
                        IsViability = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.TotalCellsMl,
                        Name = "Total Cells/ml",
                        DisplayOrder = 4,
                        IsSelected = true,
                        IsViability = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.CountsMl,
                        Name = "Counts/ml",
                        DisplayOrder = 5,
                        IsSelected = true,
                        IsFreeRanges = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.TotalCountsMl,
                        Name = "Total Counts/ml",
                        DisplayOrder = 6,
                        IsSelected = true,
                        IsFreeRanges = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.Viability,
                        Name = "% Viability",
                        DisplayOrder = 7,
                        IsSelected = true,
                        IsViability = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.ViableAggrFactor,
                        Name = "Aggr. Factor Viable Cells",
                        DisplayOrder = 8,
                        IsSelected = true,
                        IsViability = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.DebrisMl,
                        Name = "Debris / ml",
                        DisplayOrder = 8,
                        IsSelected = true,
                        IsViability = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.CellsPercentage,
                        Name = "% Cells",
                        DisplayOrder = 9,
                        IsSelected = true,
                        IsFreeRanges = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.AggrFactor,
                        Name = "Aggr. Factor",
                        DisplayOrder = 10,
                        IsSelected = true,
                        IsFreeRanges = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.CountsMlAboveRange,
                        Name = "Counts/ml > Max Range",
                        DisplayOrder = 11,
                        IsSelected = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.VolumeMlViable,
                        Name = "Volume/ml Viable Cells",
                        DisplayOrder = 12,
                        IsSelected = true,
                        IsViability = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.MeanDiameterViable,
                        Name = "Mean Diameter Viable Cells",
                        DisplayOrder = 13,
                        IsSelected = true,
                        IsViability = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.PeakDiameterViable,
                        Name = "Peak Diameter Viable Cells",
                        DisplayOrder = 13,
                        IsSelected = true,
                        IsViability = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.MeanVolumeViable,
                        Name = "Mean Volume Viable Cells",
                        DisplayOrder = 14,
                        IsSelected = true,
                        IsViability = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.PeakVolumeViable,
                        Name = "Peak Volume Viable Cells",
                        DisplayOrder = 15,
                        IsSelected = true,
                        IsViability = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.VolumeMl,
                        Name = "Volume/ml",
                        DisplayOrder = 16,
                        IsSelected = true,
                        IsFreeRanges = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.MeanDiameter,
                        Name = "Mean Diameter",
                        DisplayOrder = 17,
                        IsSelected = true,
                        IsFreeRanges = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.PeakDiameter,
                        Name = "Peak Diameter",
                        DisplayOrder = 18,
                        IsSelected = true,
                        IsFreeRanges = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.MeanVolume,
                        Name = "Mean Volume",
                        DisplayOrder = 19,
                        IsSelected = true,
                        IsFreeRanges = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.PeakVolume,
                        Name = "Peak Volume",
                        DisplayOrder = 20,
                        IsSelected = true,
                        IsFreeRanges = true,
                        Category = DocumentSettingCategory.Result
                    });

                    blobData = SerializeDocumentSettings(documentSettings);
                    _databaseStorageService.SaveSetting("OverlayDocumentSettings", blobData);
                    break;
                case DocumentType.Mean:
                    documentSettings = new List<DocumentSetting>();
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.SampleVolume,
                        Name = "Sample Volume",
                        DisplayOrder = 0,
                        IsSelected = true,
                        Category = DocumentSettingCategory.Parameter
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.DilutionFactor,
                        Name = "Dilution Factor",
                        DisplayOrder = 1,
                        IsSelected = true,
                        Category = DocumentSettingCategory.Parameter
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.Capillary,
                        Name = "Capillary",
                        DisplayOrder = 2,
                        IsSelected = true,
                        Category = DocumentSettingCategory.Parameter
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.Smoothing,
                        Name = "Smoothing",
                        DisplayOrder = 3,
                        IsSelected = true,
                        Category = DocumentSettingCategory.Parameter
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.AggrCorrection,
                        Name = "Aggr. Correction",
                        DisplayOrder = 4,
                        IsSelected = true,
                        Category = DocumentSettingCategory.Parameter
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.SizeScale,
                        Name = "Size Scale",
                        DisplayOrder = 5,
                        IsSelected = true,
                        Category = DocumentSettingCategory.Parameter
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.Ranges,
                        Name = "Ranges",
                        DisplayOrder = 6,
                        IsSelected = true,
                        Category = DocumentSettingCategory.Parameter
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.Concentration,
                        Name = "Concentration",
                        DisplayOrder = 0,
                        IsSelected = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.Counts,
                        Name = "Counts",
                        DisplayOrder = 1,
                        IsSelected = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.ViableCellsMl,
                        Name = "Viable Cells/ml",
                        DisplayOrder = 2,
                        IsSelected = true,
                        IsViability = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.DeadCellsMl,
                        Name = "Dead Cells/ml",
                        DisplayOrder = 3,
                        IsSelected = true,
                        IsViability = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.TotalCellsMl,
                        Name = "Total Cells/ml",
                        DisplayOrder = 4,
                        IsSelected = true,
                        IsViability = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.CountsMl,
                        Name = "Counts/ml",
                        DisplayOrder = 5,
                        IsSelected = true,
                        IsFreeRanges = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.TotalCountsMl,
                        Name = "Total Counts/ml",
                        DisplayOrder = 6,
                        IsSelected = true,
                        IsFreeRanges = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.Viability,
                        Name = "% Viability",
                        DisplayOrder = 7,
                        IsSelected = true,
                        IsViability = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.ViableAggrFactor,
                        Name = "Aggr. Factor Viable Cells",
                        DisplayOrder = 8,
                        IsSelected = true,
                        IsViability = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.DebrisMl,
                        Name = "Debris / ml",
                        DisplayOrder = 8,
                        IsSelected = true,
                        IsViability = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.CellsPercentage,
                        Name = "% Cells",
                        DisplayOrder = 9,
                        IsSelected = true,
                        IsFreeRanges = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.AggrFactor,
                        Name = "Aggr. Factor",
                        DisplayOrder = 10,
                        IsSelected = true,
                        IsFreeRanges = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.CountsMlAboveRange,
                        Name = "Counts/ml > Max Range",
                        DisplayOrder = 11,
                        IsSelected = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.VolumeMlViable,
                        Name = "Volume/ml Viable Cells",
                        DisplayOrder = 12,
                        IsSelected = true,
                        IsViability = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.MeanDiameterViable,
                        Name = "Mean Diameter Viable Cells",
                        DisplayOrder = 13,
                        IsSelected = true,
                        IsViability = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.PeakDiameterViable,
                        Name = "Peak Diameter Viable Cells",
                        DisplayOrder = 13,
                        IsSelected = true,
                        IsViability = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.MeanVolumeViable,
                        Name = "Mean Volume Viable Cells",
                        DisplayOrder = 14,
                        IsSelected = true,
                        IsViability = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.PeakVolumeViable,
                        Name = "Peak Volume Viable Cells",
                        DisplayOrder = 15,
                        IsSelected = true,
                        IsViability = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.VolumeMl,
                        Name = "Volume/ml",
                        DisplayOrder = 16,
                        IsSelected = true,
                        IsFreeRanges = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.MeanDiameter,
                        Name = "Mean Diameter",
                        DisplayOrder = 17,
                        IsSelected = true,
                        IsFreeRanges = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.PeakDiameter,
                        Name = "Peak Diameter",
                        DisplayOrder = 18,
                        IsSelected = true,
                        IsFreeRanges = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.MeanVolume,
                        Name = "Mean Volume",
                        DisplayOrder = 19,
                        IsSelected = true,
                        IsFreeRanges = true,
                        Category = DocumentSettingCategory.Result
                    });
                    documentSettings.Add(new DocumentSetting
                    {
                        Type = DocumentSettingType.PeakVolume,
                        Name = "Peak Volume",
                        DisplayOrder = 20,
                        IsSelected = true,
                        IsFreeRanges = true,
                        Category = DocumentSettingCategory.Result
                    });

                    blobData = SerializeDocumentSettings(documentSettings);
                    _databaseStorageService.SaveSetting("MeanDocumentSettings", blobData);
                    break;
            }
        }
    }
}
