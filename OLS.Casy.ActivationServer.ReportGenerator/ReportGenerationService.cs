using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using OLS.Casy.ActivationServer.Data;
using PeterKottas.DotNetCore.WindowsService.Interfaces;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace OLS.Casy.ActivationServer.ReportGenerator
{
    public class ReportGenerationService : IMicroService
    {
        private IMicroServiceController _controller;
        private ILogger<ReportGenerationService> _logger;
        private IFileSystemStorageService _fileSystemStorageService;

        private System.Timers.Timer _timer;

        public ReportGenerationService()
        {
            _controller = null;
        }

        public ReportGenerationService(IMicroServiceController controller, ILogger<ReportGenerationService> logger, IFileSystemStorageService fileSystemStorageService)
        {
            _controller = controller;
            _logger = logger;
            _fileSystemStorageService = fileSystemStorageService;
        }

        public void Start()
        {
            _timer = new System.Timers.Timer(1000) {AutoReset = true};
            _timer = new System.Timers.Timer(60000) { AutoReset = true };
            _timer.Elapsed += (sender, args) =>
            {
                using (var context = new ActivationContext())
                {
                    var settingEntity = context.Settings.FirstOrDefault(s => s.SettingsKey == "LastReportDate");

                    if (settingEntity == null)
                    {
                        return;
                    }

                    var lastSettingDate = DateTime.Parse(settingEntity.SettingsValue);
                    if ((DateTime.Now - lastSettingDate).TotalDays < 14)
                    {
                        return;
                    }

                    settingEntity.SettingsValue = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
                    context.SaveChanges();

                    var activationKeys = context.ActivationKey
                        .Include(d => d.Customer)
                        .Include(d => d.ActivatedMachine).ToList();

                    var activationKeyGroups = activationKeys
                        .Where(ak => ak.Responsible != null && !string.IsNullOrEmpty(ak.Responsible))
                        .GroupBy(ak => ak.Responsible)
                        .ToList();

                
                    FileInfo generalFile = new FileInfo($"General_TTT_Report.xlsx");
                    if (generalFile.Exists)
                    {
                        _fileSystemStorageService.DeleteFileAsync(generalFile.Name).Wait();
                        generalFile = new FileInfo($"General_TTT_Report.xlsx");
                    }

                    using (ExcelPackage generalPackage = new ExcelPackage(generalFile))
                    {
                        ExcelWorksheet generalWorksheet = generalPackage.Workbook.Worksheets.Add("TTT device report");
                        int k = 1;
                        int jGeneral = 2;
                        generalWorksheet.Cells[1, k].Value = "Verantwortlicher";
                        generalWorksheet.Cells[1, k++].Style.Font.Bold = true;
                        generalWorksheet.Cells[1, k].Value = "Schlüssel";
                        generalWorksheet.Cells[1, k++].Style.Font.Bold = true;
                        generalWorksheet.Cells[1, k].Value = "Gültig ab";
                        generalWorksheet.Cells[1, k++].Style.Font.Bold = true;
                        generalWorksheet.Cells[1, k].Value = "Gültig bis";
                        generalWorksheet.Cells[1, k++].Style.Font.Bold = true;
                        generalWorksheet.Cells[1, k].Value = "# Aktivierungen";
                        generalWorksheet.Cells[1, k++].Style.Font.Bold = true;
                        generalWorksheet.Cells[1, k].Value = "Gültige Seriennummer";
                        generalWorksheet.Cells[1, k++].Style.Font.Bold = true;
                        generalWorksheet.Cells[1, k].Value = "Kunde";
                        generalWorksheet.Cells[1, k++].Style.Font.Bold = true;
                        generalWorksheet.Cells[1, k].Value = "Mail";
                        generalWorksheet.Cells[1, k++].Style.Font.Bold = true;
                        generalWorksheet.Cells[1, k].Value = "Genutzt?";
                        generalWorksheet.Cells[1, k++].Style.Font.Bold = true;
                        generalWorksheet.Cells[1, k].Value = "Version";
                        generalWorksheet.Cells[1, k++].Style.Font.Bold = true;
                        generalWorksheet.Cells[1, k].Value = "Letztes Update am";
                        generalWorksheet.Cells[1, k++].Style.Font.Bold = true;
                        generalWorksheet.Cells[1, k].Value = "Steuerung";
                        generalWorksheet.Cells[1, k++].Style.Font.Bold = true;
                        generalWorksheet.Cells[1, k].Value = "Simulator";
                        generalWorksheet.Cells[1, k++].Style.Font.Bold = true;
                        generalWorksheet.Cells[1, k].Value = "AD Auth";
                        generalWorksheet.Cells[1, k++].Style.Font.Bold = true;
                        generalWorksheet.Cells[1, k].Value = "Lokale Auth";
                        generalWorksheet.Cells[1, k++].Style.Font.Bold = true;
                        generalWorksheet.Cells[1, k].Value = "Erw. Zugr. St.";
                        generalWorksheet.Cells[1, k++].Style.Font.Bold = true;
                        generalWorksheet.Cells[1, k].Value = "Counter";
                        generalWorksheet.Cells[1, k++].Style.Font.Bold = true;
                        generalWorksheet.Cells[1, k].Value = "CFR";
                        generalWorksheet.Cells[1, k++].Style.Font.Bold = true;
                        generalWorksheet.Cells[1, k].Value = "Trial";
                        generalWorksheet.Cells[1, k].Style.Font.Bold = true;

                        foreach (var activationKeyGroup in activationKeyGroups)
                        {
                            try
                            {
                                if (string.IsNullOrEmpty(activationKeyGroup.Key))
                                {
                                    continue;
                                }

                                MailAddress mailAddress = new MailAddress(activationKeyGroup.Key);

                                var split = mailAddress.Address.Split('.');

                                var firstName = split.Length >= 2 ? split[0] : "Unknown";
                                var lastName = split.Length >= 2 ? split[1].Split('@')[0] : "Unknown";

                                FileInfo file = new FileInfo($"{firstName}_{lastName}_TTT_Report.xlsx");
                                if (file.Exists)
                                {
                                    _fileSystemStorageService.DeleteFileAsync(file.Name).Wait();
                                    file = new FileInfo($"{firstName}_{lastName}_TTT_Report.xlsx");
                                }

                                int j = 2;
                                using (ExcelPackage package = new ExcelPackage(file))
                                {
                                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("TTT device report");
                                    k = 1;
                                    worksheet.Cells[1, k].Value = "Verantwortlicher";
                                    worksheet.Cells[1, k++].Style.Font.Bold = true;
                                    worksheet.Cells[1, k].Value = "Schlüssel";
                                    worksheet.Cells[1, k++].Style.Font.Bold = true;
                                    worksheet.Cells[1, k].Value = "Gültig ab";
                                    worksheet.Cells[1, k++].Style.Font.Bold = true;
                                    worksheet.Cells[1, k].Value = "Gültig bis";
                                    worksheet.Cells[1, k++].Style.Font.Bold = true;
                                    worksheet.Cells[1, k].Value = "# Aktivierungen";
                                    worksheet.Cells[1, k++].Style.Font.Bold = true;
                                    worksheet.Cells[1, k].Value = "Gültige Seriennummer";
                                    worksheet.Cells[1, k++].Style.Font.Bold = true;
                                    worksheet.Cells[1, k].Value = "Kunde";
                                    worksheet.Cells[1, k++].Style.Font.Bold = true;
                                    worksheet.Cells[1, k].Value = "Mail";
                                    worksheet.Cells[1, k++].Style.Font.Bold = true;
                                    worksheet.Cells[1, k].Value = "Genutzt?";
                                    worksheet.Cells[1, k++].Style.Font.Bold = true;
                                    worksheet.Cells[1, k].Value = "Version";
                                    worksheet.Cells[1, k++].Style.Font.Bold = true;
                                    worksheet.Cells[1, k].Value = "Letztes Update am";
                                    worksheet.Cells[1, k++].Style.Font.Bold = true;
                                    worksheet.Cells[1, k].Value = "Steuerung";
                                    worksheet.Cells[1, k++].Style.Font.Bold = true;
                                    worksheet.Cells[1, k].Value = "Simulator";
                                    worksheet.Cells[1, k++].Style.Font.Bold = true;
                                    worksheet.Cells[1, k].Value = "AD Auth";
                                    worksheet.Cells[1, k++].Style.Font.Bold = true;
                                    worksheet.Cells[1, k].Value = "Lokale Auth";
                                    worksheet.Cells[1, k++].Style.Font.Bold = true;
                                    worksheet.Cells[1, k].Value = "Erw. Zugr. St.";
                                    worksheet.Cells[1, k++].Style.Font.Bold = true;
                                    worksheet.Cells[1, k].Value = "Counter";
                                    worksheet.Cells[1, k++].Style.Font.Bold = true;
                                    worksheet.Cells[1, k].Value = "CFR";
                                    worksheet.Cells[1, k++].Style.Font.Bold = true;
                                    worksheet.Cells[1, k].Value = "Trial";
                                    worksheet.Cells[1, k].Style.Font.Bold = true;

                                    foreach (var item in activationKeyGroup)
                                    {
                                        k = 1;
                                        worksheet.Cells[j, k].Value = item.Responsible;
                                        generalWorksheet.Cells[jGeneral, k++].Value = item.Responsible;

                                        worksheet.Cells[j, k].Value = item.Value;
                                        generalWorksheet.Cells[jGeneral, k++].Value = item.Value;

                                        worksheet.Cells[j, k].Value =
                                            item.ValidFrom.ToString("dd.MM.yyyy HH':'mm':'ss");
                                        generalWorksheet.Cells[jGeneral, k++].Value =
                                            item.ValidFrom.ToString("dd.MM.yyyy HH':'mm':'ss");

                                        worksheet.Cells[j, k].Value = item.ValidTo.ToString("dd.MM.yyyy HH':'mm':'ss");
                                        generalWorksheet.Cells[jGeneral, k].Value =
                                            item.ValidTo.ToString("dd.MM.yyyy HH':'mm':'ss");

                                        if (DateTime.Now > item.ValidTo)
                                        {
                                            worksheet.Cells[j, k].Style.Font.Color.SetColor(Color.DarkRed);
                                            worksheet.Cells[j, k].Style.Fill.PatternType = ExcelFillStyle.Solid;
                                            worksheet.Cells[j, k].Style.Fill.BackgroundColor
                                                .SetColor(Color.PaleVioletRed);

                                            generalWorksheet.Cells[jGeneral, k].Style.Font.Color.SetColor(Color.DarkRed);
                                            generalWorksheet.Cells[jGeneral, k].Style.Fill.PatternType = ExcelFillStyle.Solid;
                                            generalWorksheet.Cells[jGeneral, k].Style.Fill.BackgroundColor
                                                .SetColor(Color.PaleVioletRed);
                                        }
                                        else if (DateTime.Now.Add(TimeSpan.FromDays(60)) > item.ValidTo)
                                        {
                                            worksheet.Cells[j, k].Style.Font.Color.SetColor(Color.Orange);
                                            worksheet.Cells[j, k].Style.Fill.PatternType = ExcelFillStyle.Solid;
                                            worksheet.Cells[j, k].Style.Fill.BackgroundColor.SetColor(Color.Black);

                                            generalWorksheet.Cells[jGeneral, k].Style.Font.Color.SetColor(Color.Orange);
                                            generalWorksheet.Cells[jGeneral, k].Style.Fill.PatternType = ExcelFillStyle.Solid;
                                            generalWorksheet.Cells[jGeneral, k].Style.Fill.BackgroundColor
                                                .SetColor(Color.Black);
                                        }

                                        k = k + 1;

                                        worksheet.Cells[j, k].Value = item.MaxNumActivations.ToString();
                                        generalWorksheet.Cells[jGeneral, k++].Value = item.MaxNumActivations.ToString();

                                        worksheet.Cells[j, k].Value = item.SerialNumbers;
                                        generalWorksheet.Cells[jGeneral, k++].Value = item.SerialNumbers;

                                        worksheet.Cells[j, k].Value = item.Customer.Name;
                                        generalWorksheet.Cells[jGeneral, k++].Value = item.Customer.Name;

                                        worksheet.Cells[j, k].Value = item.Customer.Mail;
                                        generalWorksheet.Cells[jGeneral, k++].Value = item.Customer.Mail;

                                        if (item.ActivatedMachine.Count > 0)
                                        {
                                            worksheet.Cells[j, k].Value = "ja";
                                            worksheet.Cells[j, k].Style.Font.Color.SetColor(Color.DarkGreen);
                                            worksheet.Cells[j, k].Style.Fill.PatternType = ExcelFillStyle.Solid;
                                            worksheet.Cells[j, k].Style.Fill.BackgroundColor.SetColor(Color.LightGreen);

                                            generalWorksheet.Cells[jGeneral, k].Value = "ja";
                                            generalWorksheet.Cells[jGeneral, k].Style.Font.Color.SetColor(Color.DarkGreen);
                                            generalWorksheet.Cells[jGeneral, k].Style.Fill.PatternType = ExcelFillStyle.Solid;
                                            generalWorksheet.Cells[jGeneral, k++].Style.Fill.BackgroundColor
                                                .SetColor(Color.LightGreen);

                                            var activatedMachines = item.ActivatedMachine
                                                .Where(am => !string.IsNullOrEmpty(am.CurrentVersion)).ToList();

                                            if (activatedMachines.Any())
                                            {
                                                var minVersion =
                                                    activatedMachines.Min(am => new Version(am.CurrentVersion));
                                                worksheet.Cells[j, k].Value = minVersion;
                                                generalWorksheet.Cells[jGeneral, k++].Value = minVersion;

                                                var minMachines = activatedMachines.Where(am =>
                                                    am.CurrentVersion == minVersion.ToString()).ToList();

                                                if (minMachines.Any())
                                                {
                                                    var minUpdate =
                                                        minMachines.Min(am => am.LastUpdatedAt);

                                                    worksheet.Cells[j, k].Value =
                                                        minUpdate.ToString("dd.MM.yyyy HH':'mm':'ss");
                                                    generalWorksheet.Cells[jGeneral, k].Value =
                                                        minUpdate.ToString("dd.MM.yyyy HH':'mm':'ss");
                                                }

                                                k++;
                                            }
                                        }
                                        else
                                        {
                                            worksheet.Cells[j, k].Value = "nein";
                                            worksheet.Cells[j, k].Style.Font.Color.SetColor(Color.DarkRed);
                                            worksheet.Cells[j, k].Style.Fill.PatternType = ExcelFillStyle.Solid;
                                            worksheet.Cells[j, k].Style.Fill.BackgroundColor
                                                .SetColor(Color.PaleVioletRed);

                                            generalWorksheet.Cells[jGeneral, k].Value = "nein";
                                            generalWorksheet.Cells[jGeneral, k].Style.Font.Color.SetColor(Color.DarkRed);
                                            generalWorksheet.Cells[jGeneral, k].Style.Fill.PatternType = ExcelFillStyle.Solid;
                                            generalWorksheet.Cells[jGeneral, k++].Style.Fill.BackgroundColor
                                                .SetColor(Color.PaleVioletRed);

                                            k++;
                                            k++;
                                        }

                                        worksheet.Cells[j, k].Value = "Aus";
                                        generalWorksheet.Cells[jGeneral, k++].Value = "Aus";

                                        worksheet.Cells[j, k].Value = "Aus";
                                        generalWorksheet.Cells[jGeneral, k++].Value = "Aus";

                                        worksheet.Cells[j, k].Value = "Aus";
                                        generalWorksheet.Cells[jGeneral, k++].Value = "Aus";

                                        worksheet.Cells[j, k].Value = "Aus";
                                        generalWorksheet.Cells[jGeneral, k++].Value = "Aus";

                                        worksheet.Cells[j, k].Value = "Aus";
                                        generalWorksheet.Cells[jGeneral, k++].Value = "Aus";

                                        worksheet.Cells[j, k].Value = "Aus";
                                        generalWorksheet.Cells[jGeneral, k++].Value = "Aus";

                                        worksheet.Cells[j, k].Value = "Aus";
                                        generalWorksheet.Cells[jGeneral, k++].Value = "Aus";

                                        worksheet.Cells[j, k].Value = "Aus";
                                        generalWorksheet.Cells[jGeneral, k++].Value = "Aus";

                                        var addOnsMappings = context.ActivationKeyAddOnMapping
                                            .Include(d => d.ProductAddOn)
                                            .Where(m => m.ActivationKeyId == item.Id).Select(d => d.ProductAddOn.Id)
                                            .ToList();

                                        foreach (var addOn in addOnsMappings)
                                        {
                                            switch (addOn)
                                            {
                                                case 1: //AD Auth
                                                    worksheet.Cells[j, 14].Value = "Aktiv";
                                                    worksheet.Cells[j, 14].Style.Font.Color.SetColor(Color.DarkGreen);
                                                    worksheet.Cells[j, 14].Style.Fill.PatternType =
                                                        ExcelFillStyle.Solid;
                                                    worksheet.Cells[j, 14].Style.Fill.BackgroundColor
                                                        .SetColor(Color.LightGreen);

                                                    generalWorksheet.Cells[jGeneral, 14].Value = "Aktiv";
                                                    generalWorksheet.Cells[jGeneral, 14].Style.Font.Color
                                                        .SetColor(Color.DarkGreen);
                                                    generalWorksheet.Cells[jGeneral, 14].Style.Fill.PatternType =
                                                        ExcelFillStyle.Solid;
                                                    generalWorksheet.Cells[jGeneral, 14].Style.Fill.BackgroundColor
                                                        .SetColor(Color.LightGreen);
                                                    break;
                                                case 2: //Control
                                                    worksheet.Cells[j, 12].Value = "Aktiv";
                                                    worksheet.Cells[j, 12].Style.Font.Color.SetColor(Color.DarkGreen);
                                                    worksheet.Cells[j, 12].Style.Fill.PatternType =
                                                        ExcelFillStyle.Solid;
                                                    worksheet.Cells[j, 12].Style.Fill.BackgroundColor
                                                        .SetColor(Color.LightGreen);

                                                    generalWorksheet.Cells[jGeneral, 12].Value = "Aktiv";
                                                    generalWorksheet.Cells[jGeneral, 12].Style.Font.Color
                                                        .SetColor(Color.DarkGreen);
                                                    generalWorksheet.Cells[jGeneral, 12].Style.Fill.PatternType =
                                                        ExcelFillStyle.Solid;
                                                    generalWorksheet.Cells[jGeneral, 12].Style.Fill.BackgroundColor
                                                        .SetColor(Color.LightGreen);
                                                    break;
                                                case 3: //Counter
                                                    worksheet.Cells[j, 17].Value = "Aktiv";
                                                    worksheet.Cells[j, 17].Style.Font.Color.SetColor(Color.DarkGreen);
                                                    worksheet.Cells[j, 17].Style.Fill.PatternType =
                                                        ExcelFillStyle.Solid;
                                                    worksheet.Cells[j, 17].Style.Fill.BackgroundColor
                                                        .SetColor(Color.LightGreen);

                                                    generalWorksheet.Cells[jGeneral, 17].Value = "Aktiv";
                                                    generalWorksheet.Cells[jGeneral, 17].Style.Font.Color
                                                        .SetColor(Color.DarkGreen);
                                                    generalWorksheet.Cells[jGeneral, 17].Style.Fill.PatternType =
                                                        ExcelFillStyle.Solid;
                                                    generalWorksheet.Cells[jGeneral, 17].Style.Fill.BackgroundColor
                                                        .SetColor(Color.LightGreen);
                                                    break;
                                                case 4: //local auth
                                                    worksheet.Cells[j, 15].Value = "Aktiv";
                                                    worksheet.Cells[j, 15].Style.Font.Color.SetColor(Color.DarkGreen);
                                                    worksheet.Cells[j, 15].Style.Fill.PatternType =
                                                        ExcelFillStyle.Solid;
                                                    worksheet.Cells[j, 15].Style.Fill.BackgroundColor
                                                        .SetColor(Color.LightGreen);

                                                    generalWorksheet.Cells[jGeneral, 15].Value = "Aktiv";
                                                    generalWorksheet.Cells[jGeneral, 15].Style.Font.Color
                                                        .SetColor(Color.DarkGreen);
                                                    generalWorksheet.Cells[jGeneral, 15].Style.Fill.PatternType =
                                                        ExcelFillStyle.Solid;
                                                    generalWorksheet.Cells[jGeneral, 15].Style.Fill.BackgroundColor
                                                        .SetColor(Color.LightGreen);
                                                    break;
                                                case 5: //local auth
                                                    worksheet.Cells[j, 13].Value = "Aktiv";
                                                    worksheet.Cells[j, 13].Style.Font.Color.SetColor(Color.DarkGreen);
                                                    worksheet.Cells[j, 13].Style.Fill.PatternType =
                                                        ExcelFillStyle.Solid;
                                                    worksheet.Cells[j, 13].Style.Fill.BackgroundColor
                                                        .SetColor(Color.LightGreen);

                                                    generalWorksheet.Cells[jGeneral, 13].Value = "Aktiv";
                                                    generalWorksheet.Cells[jGeneral, 13].Style.Font.Color
                                                        .SetColor(Color.DarkGreen);
                                                    generalWorksheet.Cells[jGeneral, 13].Style.Fill.PatternType =
                                                        ExcelFillStyle.Solid;
                                                    generalWorksheet.Cells[jGeneral, 13].Style.Fill.BackgroundColor
                                                        .SetColor(Color.LightGreen);
                                                    break;
                                                case 6: //tt switch
                                                    break;
                                                case 7: //CFR
                                                    worksheet.Cells[j, 18].Value = "Aktiv";
                                                    worksheet.Cells[j, 18].Style.Font.Color.SetColor(Color.DarkGreen);
                                                    worksheet.Cells[j, 18].Style.Fill.PatternType =
                                                        ExcelFillStyle.Solid;
                                                    worksheet.Cells[j, 18].Style.Fill.BackgroundColor
                                                        .SetColor(Color.LightGreen);

                                                    generalWorksheet.Cells[jGeneral, 18].Value = "Aktiv";
                                                    generalWorksheet.Cells[jGeneral, 18].Style.Font.Color
                                                        .SetColor(Color.DarkGreen);
                                                    generalWorksheet.Cells[jGeneral, 18].Style.Fill.PatternType =
                                                        ExcelFillStyle.Solid;
                                                    generalWorksheet.Cells[jGeneral, 18].Style.Fill.BackgroundColor
                                                        .SetColor(Color.LightGreen);
                                                    break;
                                                case 9: //CFR
                                                    worksheet.Cells[j, 19].Value = "Aktiv";
                                                    worksheet.Cells[j, 19].Style.Font.Color.SetColor(Color.DarkGreen);
                                                    worksheet.Cells[j, 19].Style.Fill.PatternType =
                                                        ExcelFillStyle.Solid;
                                                    worksheet.Cells[j, 19].Style.Fill.BackgroundColor
                                                        .SetColor(Color.LightGreen);

                                                    generalWorksheet.Cells[jGeneral, 19].Value = "Aktiv";
                                                    generalWorksheet.Cells[jGeneral, 19].Style.Font.Color
                                                        .SetColor(Color.DarkGreen);
                                                    generalWorksheet.Cells[jGeneral, 19].Style.Fill.PatternType =
                                                        ExcelFillStyle.Solid;
                                                    generalWorksheet.Cells[jGeneral, 19].Style.Fill.BackgroundColor
                                                        .SetColor(Color.LightGreen);
                                                    break;
                                                case 10: //Access
                                                    worksheet.Cells[j, 16].Value = "Aktiv";
                                                    worksheet.Cells[j, 16].Style.Font.Color.SetColor(Color.DarkGreen);
                                                    worksheet.Cells[j, 16].Style.Fill.PatternType =
                                                        ExcelFillStyle.Solid;
                                                    worksheet.Cells[j, 16].Style.Fill.BackgroundColor
                                                        .SetColor(Color.LightGreen);

                                                    generalWorksheet.Cells[jGeneral, 16].Value = "Aktiv";
                                                    generalWorksheet.Cells[jGeneral, 16].Style.Font.Color
                                                        .SetColor(Color.DarkGreen);
                                                    generalWorksheet.Cells[jGeneral, 16].Style.Fill.PatternType =
                                                        ExcelFillStyle.Solid;
                                                    generalWorksheet.Cells[jGeneral, 16].Style.Fill.BackgroundColor
                                                        .SetColor(Color.LightGreen);
                                                    break;
                                            }
                                        }
                                    j++;
                                    jGeneral++;
                                    }

                                    for (var i = 1; i <= worksheet.Dimension.End.Column; i++)
                                    {
                                        worksheet.Column(i).AutoFit();
                                    }

                                    package.Save();
                                    //}

                                    SendMail(mailAddress.Address, file, firstName, lastName);
                                    //SendMail("maik.windhorst@ols-bio.de", file, "Maik", "Windhorst");

                                    _logger.LogInformation("Workbook created");
                                }
                            }
                            catch
                            {
                            }
                        }

                        for (var i = 1; i <= generalWorksheet.Dimension.End.Column; i++)
                        {
                            generalWorksheet.Column(i).AutoFit();
                        }

                        generalPackage.Save();

                        //SendMail(mailAddress.Address, file, firstName, lastName);
                        SendMail("andreas.friese@ols-bio.de", generalFile, "Andreas", "Friese");
                    }
                }
            };

            _timer.Start();
            _logger.LogTrace("Started\n");
        }

        public void Stop()
        {
            _timer.Stop();
            _timer.Dispose();
            _logger.LogTrace("Stopped\n");
        }

        public void SendMail(string address, FileInfo attachment, string firstName, string lastName)
        {
            using (MailMessage message = new MailMessage
            {
                From = new MailAddress("noreply@ols-bio.de", "CASY Reporting Service"),
                Sender = new MailAddress("noreply@ols-bio.de", "CASY Reporting Service")
            })
            {
                ;
                message.ReplyToList.Add(new MailAddress("noreply@ols-bio.de", "CASY Reporting Service"));
                //foreach (var to in tos)
                //{
                //message.To.Add("maik.windhorst@ols-bio.de");
                //message.To.Add("andreas.friese@ols-bio.de");
                message.To.Add(address);
                //message.To.Add("ralf.ketterlinus@ols-bio.de");
                //}
                message.Subject =
                    $"CASY activation report for: {char.ToUpperInvariant(firstName[0]) + firstName.Substring(1)} {char.ToUpperInvariant(lastName[0]) + lastName.Substring(1)}";
                message.Body = $"Hi {char.ToUpperInvariant(firstName[0]) + firstName.Substring(1)},\n" +
                               $"\n" +
                               $"ab sofort erhälst du alle zwei Wochen einen Bericht über die verkauften CASY TTT Aktivierungsschlüssel die dir zugeordnet sind als Excel-File." +
                               "\n" +
                               "Bei Fragen/Wünschen/Anregungen wende dich an Maik.\n" +
                               "\n" +
                               "Viele Grüße\n" +
                               "Dein CASY Reporting Service";

                message.Attachments.Add(new Attachment(attachment.FullName));

                ServicePointManager.ServerCertificateValidationCallback = delegate(object s,
                    X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
                {
                    return true;
                };

                using (SmtpClient client = new SmtpClient()
                {
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Host = "olsmail.ols-bio.de",
                    Port = 25,
                    EnableSsl = true,
                    Credentials = new NetworkCredential("mwindhorst@ols-bio.de", "ZcM53211")
                })
                {
                    client.Send(message);
                }
            }
        }
    }
}
