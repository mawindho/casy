using DevExpress.Mvvm;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Windows.Input;

namespace OLS.Casy.ErrorReport.Ui
{
    public class MainViewModel : Casy.Ui.Base.ViewModelBase
    {
        private string _errorReport;

        public MainViewModel()
        {
            ReadErrorReport();
        }

        public string ErrorReport
        {
            get => _errorReport;
            set
            {
                if (value == _errorReport) return;
                _errorReport = value;
                NotifyOfPropertyChange();
            }
        }

        public ICommand ExportFileCommand => new DelegateCommand(OnExportAsFile);
        public ICommand SendAsMailCommand => new DelegateCommand(OnSendAsMail);
        
        private void OnExportAsFile()
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = "Export CASY error report";
                dialog.FileName = $"CASY Error Report {DateTime.Now:yyyy-dd-M--HH-mm-ss}.txt";

                var result = dialog.ShowDialog() == DialogResult.OK;
                if (!result) return;
                var path = dialog.FileName;
                File.WriteAllText(path, ErrorReport, Encoding.UTF8);
            }
        }

        private void OnSendAsMail()
        {
            var url = $"mailto:maik.windhorst@ols-bio.de?subject=CASY error report; Customer: <FILL IN YOUR CONTACT>&body={ErrorReport.Replace("\r\n", "%0D%0A")}";
            Process.Start(url);

            //var mailMessage = new MailMessage();
            //mailMessage.To.Add(new MailAddress(""));
            //mailMessage.Subject = "CASY error report; Customer: <FILL IN YOUR CONTACT>";
            //mailMessage.IsBodyHtml = false;
            //mailMessage.Body = ErrorReport;

            //var filename = "casyerrormessage.eml";

            //Save(mailMessage, filename);

            //Process.Start(filename);
        }

        private void ReadErrorReport()
        {
            var path = @"UnhandledException.txt";
            if (File.Exists(path))
            {
                ErrorReport = File.ReadAllText(path);
            }
        }

        public static void Save(MailMessage message, string filename, bool addUnsentHeader = true)
        {
            using (var filestream = File.Open(filename, FileMode.Create))
            {
                if (addUnsentHeader)
                {
                    var binaryWriter = new BinaryWriter(filestream);
                    //Write the Unsent header to the file so the mail client knows this mail must be presented in "New message" mode
                    binaryWriter.Write(System.Text.Encoding.UTF8.GetBytes("X-Unsent: 1" + Environment.NewLine));
                }

                var assembly = typeof(SmtpClient).Assembly;
                var mailWriterType = assembly.GetType("System.Net.Mail.MailWriter");

                // Get reflection info for MailWriter contructor
                var mailWriterContructor = mailWriterType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(Stream) }, null);

                // Construct MailWriter object with our FileStream
                var mailWriter = mailWriterContructor.Invoke(new object[] { filestream });

                // Get reflection info for Send() method on MailMessage
                var sendMethod = typeof(MailMessage).GetMethod("Send", BindingFlags.Instance | BindingFlags.NonPublic);

                sendMethod.Invoke(message, BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { mailWriter, true, true }, null);

                // Finally get reflection info for Close() method on our MailWriter
                var closeMethod = mailWriter.GetType().GetMethod("Close", BindingFlags.Instance | BindingFlags.NonPublic);

                // Call close method
                closeMethod.Invoke(mailWriter, BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { }, null);
            }
        }
    }
}
