using Microsoft.AspNetCore.Mvc;
using OLS.Casy.Core.Activation.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Linq;
using OLS.Casy.ActivationServer.Data;
using Microsoft.EntityFrameworkCore;

namespace OLS.Casy.ActivationServer.Host.Controllers
{
    [Route("api/[controller]")]
    public class UpdateController : Controller
    {
        private Data.ActivationContext _activationContext;

        public UpdateController(Data.ActivationContext activationContext)
        {
            this._activationContext = activationContext;
        }

        [HttpGet]
        [Route("{environment}/{version}/{type}")]
        public IActionResult GetUpdateFile(string environment, string version, string type)
        {
            var type2 = string.Empty;
            switch(type)
            {
                case "Desktop":
                    type2 = "Desktop";
                    break;
                case "Simulator":
                    type2 = "Simulator";
                    break;
            }

            var filePath = string.Format(@"C:\Projekte\Casy\Deployment\Updates\{0}\{1}\update{2}.zip", environment, version, type2);

            if(!System.IO.File.Exists(filePath))
            {
                return new BadRequestObjectResult("File not found");
            }

            var dataBytes = System.IO.File.ReadAllBytes(filePath);
            //adding bytes to memory stream   
            var dataStream = new MemoryStream(dataBytes);

            return new FileStreamResult(dataStream, "application/octet-stream");
        }

        [HttpGet]
        [Route("2/{environment}/{version}/{type}")]
        public IActionResult GetUpdateFile2(string environment, string version, string type)
        {
            var filePath = string.Format(@"C:\Projekte\Casy\Deployment\Updates\{0}\{1}\{2}.zip", environment, version, type);

            if (!System.IO.File.Exists(filePath))
            {
                return new BadRequestObjectResult("File not found");
            }

            var dataBytes = System.IO.File.ReadAllBytes(filePath);
            //adding bytes to memory stream   
            var dataStream = new MemoryStream(dataBytes);

            return new FileStreamResult(dataStream, "application/octet-stream");
        }

        [HttpGet]
        [Route("usbUpdate/{guid}/{serial}")]
        public IActionResult GetUsbUpdate(string guid, string serial)
        {
            var customer = _activationContext.Customer.FirstOrDefault(x => x.UpdateGuid == guid);

            if (customer == null)
            {
                return new BadRequestObjectResult("Invalid USB Update request");
            }

            var activationKeys =
                _activationContext.ActivationKey.Include("ActivationKeyProductAddOns").Include("ActivationKeyProductAddOns.ProductAddOn").Where(x => x.CustomerId == customer.Id && x.SerialNumbers == serial).ToList();

            if (!activationKeys.Any())
            {
                return new BadRequestObjectResult("Invalid USB Update request");
            }

            if (activationKeys.All(x => x.ValidTo < DateTime.Now))
            {
                return new BadRequestObjectResult("Activation key expired");
            }

            int maxCount = 0;
            ActivationKey activationKey = null;

            foreach (var key in activationKeys)
            {
                if (key.ActivationKeyProductAddOns.Count > maxCount)
                {
                    maxCount = key.ActivationKeyProductAddOns.Count;
                    activationKey = key;
                }
            }
            
            var addOns = activationKey.ActivationKeyProductAddOns.Select(x => x.ProductAddOn.Type).OrderBy(x => x);
            var addOnsPath = string.Join("_", addOns);

            var filePath = string.Format(@"C:\Projekte\Casy\Deployment\UsbUpdates\{0}\CasyUsbUpdate.zip", addOnsPath);

            if (!System.IO.File.Exists(filePath))
            {
                return new BadRequestObjectResult("File not found: " + filePath);
            }

            var dataBytes = System.IO.File.ReadAllBytes(filePath);
            //adding bytes to memory stream   
            var dataStream = new MemoryStream(dataBytes);

            return new FileStreamResult(dataStream, "application/octet-stream")
            {
                FileDownloadName = "CasyUsbUpdate.zip"
            };
        }

        [HttpPost("info")]
        public IActionResult PostUpdateInfo([FromBody] UpdateRequest updateInfo)
        {
            if(string.IsNullOrEmpty(updateInfo.ActivationKey) || string.IsNullOrEmpty(updateInfo.CpuId) || updateInfo.CurrentVersion == null)
            {
                updateInfo.RequestError = "Invalid request data";
                return new ObjectResult(updateInfo);
            }

            var activatedMachine = this._activationContext.ActivatedMachine.Include("ActivationKey").FirstOrDefault(am => am.ActivationKey.Value == updateInfo.ActivationKey && am.MacAdress == updateInfo.CpuId);
            if(activatedMachine == null)
            {
                updateInfo.RequestError = "Activated machine not found";
                return new ObjectResult(updateInfo);
            }

            var now = DateTime.UtcNow;
            activatedMachine.LastUpdatedAt = now;
            activatedMachine.CurrentVersion = updateInfo.CurrentVersion.ToString();
            _activationContext.SaveChanges();

            return new ObjectResult(updateInfo);
        }

        [HttpPost]
        public IActionResult PostUpdateRequest([FromBody] UpdateRequest updateRequest)
        {
            if (string.IsNullOrEmpty(updateRequest.ActivationKey) || updateRequest.CurrentVersion == null)
            {
                updateRequest.RequestError = "Activation key or current version missing";
                return new ObjectResult(updateRequest);
            }

            var activationKeyEntity = this._activationContext.ActivationKey.Include("ProductType").Include("Customer").Include("ProductAddOn").FirstOrDefault(ak => ak.Value == updateRequest.ActivationKey);
            if (activationKeyEntity == null)
            {
                updateRequest.RequestError = "Invalid activation key";
                return new ObjectResult(updateRequest);
            }

            var now = DateTime.UtcNow;
            if (now > activationKeyEntity.ValidTo)// && now < activationKeyEntity.ValidFrom)
            {
                //updateRequest.RequestError = "Activation has been expired";
                return new ObjectResult(updateRequest);
            }

            var updateInfoContent = System.IO.File.ReadAllText(string.Format(@"C:\Projekte\Casy\Deployment\Updates\{0}\updateInfo.xml", updateRequest.Environment));

            string updateType = string.Empty;
            if (activationKeyEntity.ProductTypeId == 2)
            {
                updateType = "Desktop";
            }
            else if(activationKeyEntity.ProductTypeId == 3)
            {
                updateType = "Counter";
            }
            else if (activationKeyEntity.ProductTypeId == 1 && activationKeyEntity.Customer.Name == "Simulator")
            {
                updateType = "Simulator";
            }
            else
            {
                updateType = "Full";
            }

            var updateVersions = CheckForUpdates(updateInfoContent, updateRequest.CurrentVersion, updateType /*, activationKeyEntity.ProductAddOn.Select(addOn => addOn.Id).ToArray()*/);
            var orderedUpdateInfos = updateVersions.OrderBy(info => new Version(info.Version)).ToList();

            if (orderedUpdateInfos.Any())
            {
                updateRequest.UpdateVersions = new List<UpdateVersion>();

                foreach (var updateVersion in orderedUpdateInfos)
                {
                    updateRequest.UpdateVersions.Add(updateVersion);
                }
            }

            return new ObjectResult(updateRequest);
        }

        private List<UpdateVersion> CheckForUpdates(string updateInfoFileContent, Version currentVersion, string type /*, int[] addOnIds*/)
        {
            List<UpdateVersion> result = new List<UpdateVersion>();

            XDocument updateInfoXml = XDocument.Parse(updateInfoFileContent);

            bool isSimulatorInstalled = type == "Simulator";
            bool isDesktopVersion = type == "Desktop";
            bool isCounterVersion = type == "Counter";

            foreach (var update in updateInfoXml.Descendants("Update"))
            {
                var versionString = update.Descendants("Version").FirstOrDefault().Value;
                var location = update.Descendants("Location").FirstOrDefault().Value;
                bool isSimulator = bool.Parse(update.Descendants("IsSimulator").FirstOrDefault().Value);
                var isDesktopXml = update.Descendants("IsDesktop").FirstOrDefault();
                bool isDesktop = isDesktopXml != null && bool.Parse(isDesktopXml.Value);
                bool forceRestart = bool.Parse(update.Descendants("ForceRestart").FirstOrDefault().Value);

                List<string> filesToDelete = new List<string>();
                var toDelete = update.Descendants("ToDelete").FirstOrDefault();
                if (toDelete != null)
                {
                    var files = toDelete.Descendants("File").ToList();

                    foreach (var file in files)
                    {
                        filesToDelete.Add(file.Value);
                    }
                }

                Version version = new Version(versionString);

                if (version > currentVersion && isSimulator == isSimulatorInstalled && isDesktop == isDesktopVersion)
                {
                    result.Add(new UpdateVersion()
                    {
                        Version = version.ToString(),
                        Location = location,
                        IsSimulator = isSimulator,
                        //IsDesktop = isDesktop,
                        ForceRestart = forceRestart,
                        FilesToDelete = filesToDelete.ToArray()
                    });
                }
            }

            return result;
        }
    }
}
