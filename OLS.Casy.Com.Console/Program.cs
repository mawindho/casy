using OLS.Casy.ActivationServer.Data;
using OLS.Casy.Controller.Api;
using OLS.Casy.Controller.Measure;
using OLS.Casy.Core;
using OLS.Casy.IO.SQLite;
using OLS.Casy.IO.SQLite.EF;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace OLS.Casy.Com.Console
{
    class Program
    {
        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        static void Main(string[] args)
        {
            /*var environmentService = new EnvironmentService();
            SQLiteDatabaseChannel channel = new SQLiteDatabaseChannel(environmentService, new List<IAuditTrailDecorator>());

            string input = "";

            while(string.IsNullOrEmpty(input))
                { 
            var experiments = channel.GetExperiments();
            foreach(var experiment in experiments)
            {
                var groups = channel.GetGroups(experiment.Item1);

                foreach(var group in groups)
                {
                    var measResults = channel.GetMeasureResults(experiment.Item1, group.Item1);
                    

                    foreach (var measResult in measResults)
                    {
                        var deep = channel.GetMeasureResult(measResult.MeasureResultId);
                        channel.LoadDisplayData(deep);

                            var random = RandomString(4);

                        deep.MeasureResultGuid = Guid.NewGuid();
                        deep.MeasureResultId = -1;
                        deep.Name = deep.Name + "_" + random; 
                        deep.MeasureSetup.MeasureSetupId = -1;
                        deep.MeasureSetup.Name = deep.MeasureSetup.Name + "_" + random;
                        foreach (var cursor in deep.MeasureSetup.Cursors)
                        {
                            cursor.CursorId = -1;
                        }
                        deep.OriginalMeasureSetup.MeasureSetupId = -1;
                        deep.OriginalMeasureSetup.Name = deep.OriginalMeasureSetup.Name + "_" + random;
                        foreach (var cursor in deep.OriginalMeasureSetup.Cursors)
                        {
                            cursor.CursorId = -1;
                        }

                        foreach(var data in deep.MeasureResultDatas)
                        {
                            data.MeasureResultDataId = -1;
                        }

                        channel.SaveMeasureResult(deep);
                    }
                }

                var fileInfo = new FileInfo("casy.db");
                System.Console.WriteLine($"Filesize: {fileInfo.Length}");
            }

          
            }*/

            SendUsbUpdateMail(new ActivationKey()
            {
                Customer = new Customer()
                {
                    Mail = "joachim.pavel@ols-bio.de",
                    UpdateGuid = Guid.NewGuid().ToString()
                },
                ValidTo = DateTime.Now.AddDays(14),
                SerialNumbers = "TTT-123-4567"
            });
        }

        private static string _currentVersion = "1.1.0.12";

        private static void SendUsbUpdateMail(ActivationKey activationKey)
        {
            try
            {
                var mailAddress = new MailAddress(activationKey.Customer.Mail);
                using (var message = new MailMessage
                {
                    From = new MailAddress("maik.windhorst@ols-bio.de", "Maik Windhorst (OMNI Life Science GmbH & Co KG)"),
                    Sender = new MailAddress("maik.windhorst@ols-bio.de", "Maik Windhorst (OMNI Life Science GmbH & Co KG)")
                })
                {
                    message.ReplyToList.Add(new MailAddress("maik.windhorst@ols-bio.de", "Maik Windhorst (OMNI Life Science GmbH & Co KG)"));
                    message.To.Add(mailAddress);
                    //message.To.Add("maik.windhorst@ols-bio.de");
                    message.Bcc.Add(new MailAddress("maik.windhorst@ols-bio.de"));
                    message.Subject = $"Update CASY Software Version {_currentVersion}";
                    message.IsBodyHtml = true;
                    message.Body = "<p style=\"font-family:'Calibri, sans-serif';\">Dear CASY User,</p>"
+ "<p style=\"font-family:'Calibri, sans-serif';\">A new version of the CASY software is available to be installed.<br />"
+ "Keeping the software up to date ensures the stable and secure operation of the device. To perform the update using and USB Stick, please follow the instructions provided below.</p>"
+ "<p style=\"font-family:'Calibri, sans-serif';\">Highlights:"
+ "<ul>"
+ "<li>New search option for measurement results by name</li>"
+ "<li>Major performance and stability enhancement</li>"
+ "</ul></p>"
+ "<p style=\"font-family:'Calibri, sans-serif';\">For detailed information about feature changes please check the ReleaseNotes.txt in installation directory after installation.</p>"
+ "<p style=\"font-family:'Calibri, sans-serif';\"><b>IMPORTANT NOTE:</b></p>"
+ "<p style=\"font-family:'Calibri, sans-serif';\">The update may take up to about 2 hours. As part of the significant performance enhancement, the software will migrate the CASY database. Therefore please plan your update at an appropriate time for you.</p>"
+ "<p style=\"font-family:'Calibri, sans-serif';\">In case of any problem, please contact us immediately for further support (<a href=\"mailto:update@ols-bio.de\">update@ols-bio.de</a>)."
+ "<p style=\"font-family:'Calibri, sans-serif';\">CASY software updates are available for all users maintaining an up-to date guarantee.</p>"
+ $"<p style=\"font-family:'Calibri, sans-serif';\">Please note that our CASY software up to date guarantee is included during the initial 12 month and can be extended. Your period ends or has been ended on <b>{activationKey.ValidTo.ToString("dd.MM.yyyy HH':'mm':'ss")}</b>."
+ "<p style=\"font-family:'Calibri, sans-serif';\">Please contact your local CASY partner for a quote request to enhance the up-to-date-guarantee or contact <a href=\"mailto:info@ols-bio.de\">info@ols-bio.de</a> and we will bring you into contact with right person.</p>"
+ "<p style=\"font-family:'Calibri, sans-serif';\">We hope you will enjoy your new CASY software experience!</p>"
+ "<p style=\"font-family:'Calibri, sans-serif';\"><strong><u>How to update:</u></strong></p>"
+ "<p style=\"font-family:'Calibri, sans-serif';\"><ol>"
+ "<li>Your CASY control unit connected to the internet will have automatic access to the update server when you start the software soon. Until then, you can install the update via USB stick as described below.</li>"
+ "<li>You can install the update using a USB stick as described below.</li></ol></p>"
+ "<p style=\"font-family:'Calibri, sans-serif';\"><b>USB UPDATE process:</b></p>"
+ "<p style=\"font-family:'Calibri, sans-serif';\"><b>IMPORTANT:</b> This update is a \"two-step\" update. This means you have to plugin the USB stick <b>TWO</b> times <br />"
+ "Update 1 --> Version 1.1.0.1<br />"
+ "Update 2 --> Version 1.1.0.12</p>"
+ "<ol>"
+ "<li>The update USB stick must be prepared on a Windows PC!</li>"
+ $"<li>Download the ZIP file using this link: <a href=\"http://update.ols-bio.de/api/update/usbUpdate/{activationKey.Customer.UpdateGuid}/{activationKey.SerialNumbers}\">http://update.ols-bio.de/api/update/usbUpdate/{activationKey.Customer.UpdateGuid}/{activationKey.SerialNumbers}</a><br>"
+ "<b>Please note:</b>It may be neccessary to accept the security protocol of our german OLS CASY update server </li>"
+ "<li>Unzip the file with the free tool <b>7Zip</b> (<a href=\"https://www.7-zip.org/download.html\">https://www.7-zip.org/download.html</a>) to an empty USB stick<br />"
+ "<b>Please ensure:</b> Do NOT extract into a folder, the file “updateVersion.xml” must be in the root directory of the USB stick</li>"
+ "<li>Start the CASY software and log in with an account with supervisor privileges. Plug in the USB stick and the update will be detected automatically.</li>"
+ "</ol><p>Feel free to contact our service (<a href=\"mailto:service@ols-bio.de\">service@ols-bio.de</a>) or me directly if you need further assistance, have further questions or ideas/demands for future CASY software versions.</p>"
+ "<p style=\"font-family:'Calibri, sans-serif';\">------</p>"
+ "<p style=\"font-family:'Calibri, sans-serif';\">Best regards</p>"
+ "<p style=\"font-family:'Calibri, sans-serif';\">Maik Windhorst | OLS - OMNI Life Science GmbH &amp;Co KG<br>"
+ "Head of Software Development<br>"
+ "T +49 421 276 169 0<br>"
+ "<a href=\"mailto:maik.windhorst@ols-bio.de\">maik.windhorst@ols-bio.de</a> | <a href=\"http://www.ols-bio.de\">www.ols-bio.de</a></p>"
+ "<p style=\"font-family:'Calibri, sans-serif';\">Karl-Ferdinand-Braun-Stra&szlig;e 2 | 28359 Bremen | Germany | Gesch&auml;ftsf&uuml;hrer: Dagmar J&uuml;rgens | Amtsgericht Bremen, HRA 23428</p>";

                    ServicePointManager.ServerCertificateValidationCallback =
                        (s, certificate, chain, sslPolicyErrors) => true;

                    using (var client = new SmtpClient
                    {
                        Host = "192.168.110.3",
                        Port = 587,
                        EnableSsl = true,
                        Credentials = new NetworkCredential("mwindhorst", "ZcM5321!QdV96$")
                    })
                    {
                        client.Send(message);
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}
