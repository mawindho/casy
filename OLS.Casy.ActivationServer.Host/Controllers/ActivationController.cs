using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OLS.Casy.ActivationServer.Data;
using OLS.Casy.Core.Activation.Model;
using System;
using System.IO;
using System.Linq;

namespace OLS.Casy.ActivationServer.Host.Controllers
{
    [Route("api/[controller]")]
    public class ActivationController : Controller
    {
        private Data.ActivationContext _activationContext;

        public ActivationController(Data.ActivationContext activationContext)
        {
            this._activationContext = activationContext;
        }

        [HttpGet]
        [Route("calib/{serialNumber}")]
        public IActionResult GetUpdateFile(string serialNumber)
        {
            var filePath = string.Format(@"C:\Projekte\Casy\Deployment\Calibrations\{0}\{0}.zip", serialNumber);

            if (!System.IO.File.Exists(filePath))
            {
                return new BadRequestObjectResult("File not found");
            }

            var dataBytes = System.IO.File.ReadAllBytes(filePath);
            //adding bytes to memory stream   
            var dataStream = new MemoryStream(dataBytes);

            return new FileStreamResult(dataStream, "application/octet-stream");
        }

        [HttpPost]
        public IActionResult Post([FromBody] ActivationModel activationModel)
        {
            if(string.IsNullOrEmpty(activationModel.ActivationKey) || string.IsNullOrEmpty(activationModel.CpuId))
            {
                activationModel.ValidationError = "Activation key and/or cpu id must not be empty";
                return new ObjectResult(activationModel);
            }

            var activationKeyEntity = this._activationContext.ActivationKey.Include("ProductType").Include("ActivatedMachine").Include("ProductAddOn").FirstOrDefault(ak => ak.Value == activationModel.ActivationKey);
            if (activationKeyEntity == null)
            {
                activationModel.ValidationError = "Entered activation key is invalid";
                return new ObjectResult(activationModel);
            }

            //if(activationKeyEntity.ProductType.Id == 1 && string.IsNullOrEmpty(activationModel.SerialNumber))
            //{
                //activationModel.ValidationError = "No device serial number found";
                //return new ObjectResult(activationModel);
            //}

            var validSerialNumbers = string.IsNullOrEmpty(activationKeyEntity.SerialNumbers) ? new string[0] : activationKeyEntity.SerialNumbers.Split(';');
            //if(!string.IsNullOrEmpty(activationModel.SerialNumber) && !validSerialNumbers.Contains(activationModel.SerialNumber))
            //{
            //    activationModel.ValidationError = "This software version is not registered with your device.";
            //    return new ObjectResult(activationModel);
            //}
            if (validSerialNumbers.Any())
            {
                activationModel.SerialNumber = validSerialNumbers[0];
            }

            var activatedDevice = activationKeyEntity.ActivatedMachine.FirstOrDefault(am => am.MacAdress == activationModel.CpuId && am.ComputerName == activationModel.ComputerName);
            if (activatedDevice == null)
            {
                if (activationKeyEntity.MaxNumActivations < activationKeyEntity.ActivatedMachine.Count + 1)
                {
                    activationModel.ValidationError = "Entered activation key is already in use by another device";
                    return new ObjectResult(activationModel);
                }

                activatedDevice = new ActivatedMachine()
                {
                    ActivatedOn = DateTime.UtcNow,
                    ActivationKeyId = activationKeyEntity.Id,
                    MacAdress = activationModel.CpuId,
                    SerialNumber = activationModel.SerialNumber == null ? string.Empty : activationModel.SerialNumber,
                    ComputerName = activationModel.ComputerName,
                    LastUpdatedAt = DateTime.MinValue
                };

                this._activationContext.ActivatedMachine.Add(activatedDevice);
                this._activationContext.SaveChanges();
            }
            else
            {
                activationModel.ValidationError = "This software version is already registered with your device. Please contact OLS!";
                return new ObjectResult(activationModel);
            }

            activationModel.IsValid = true;
            activationModel.ValidFrom = activationKeyEntity.ValidFrom;
            activationModel.ValidTo = activationKeyEntity.ValidTo;
            activationModel.ProductType = activationKeyEntity.ProductType == null ? string.Empty : activationKeyEntity.ProductType.Type;
            activationModel.AddOns = new System.Collections.Generic.List<string>();
            //activationModel.Environment = 

            foreach(var addOn in activationKeyEntity.ActivationKeyProductAddOns)
            {
                activationModel.AddOns.Add(_activationContext.ProductAddOn.First(x => x.Id == addOn.ProductAddOnId).Type);
            }
            
            return new ObjectResult(activationModel);
        }

        [HttpPost]
        [Route("update")]
        public IActionResult UpdateActivatedMachine([FromBody] ActivationModel activationModel)
        {
            if (string.IsNullOrEmpty(activationModel.ActivationKey) || string.IsNullOrEmpty(activationModel.CpuId))
            {
                activationModel.ValidationError = "Activation key and/or cpu id must not be empty";
                return new ObjectResult(activationModel);
            }

            var activationKeyEntity = this._activationContext.ActivationKey.Include("ProductType").Include("ActivatedMachine").Include("ProductAddOn").FirstOrDefault(ak => ak.Value == activationModel.ActivationKey);
            if (activationKeyEntity == null)
            {
                activationModel.ValidationError = "Entered activation key is invalid";
                return new ObjectResult(activationModel);
            }

            var activatedDevice = activationKeyEntity.ActivatedMachine.FirstOrDefault(am => am.MacAdress == activationModel.CpuId && am.ComputerName == activationModel.ComputerName);

            if (activatedDevice != null)
            {
                activatedDevice.SerialNumber = activationModel.SerialNumber;
                this._activationContext.SaveChanges();

                activationModel.ValidTo = activationKeyEntity.ValidTo;
                activationModel.ProductType = activationKeyEntity.ProductType == null ? string.Empty : activationKeyEntity.ProductType.Type;
                activationModel.AddOns = new System.Collections.Generic.List<string>();

                foreach (var addOn in activationKeyEntity.ActivationKeyProductAddOns)
                {
                    activationModel.AddOns.Add(_activationContext.ProductAddOn.First(x => x.Id == addOn.ProductAddOnId).Type);
                }
            }
            else
            {
                activationModel.ValidationError = "Can't find activation for this device.";
            }

            return new ObjectResult(activationModel);
        }

        [HttpPost]
        [Route("counts")]
        public IActionResult GetAvailableCounts([FromBody] AvailableCounts availableCounts)
        {
            if (string.IsNullOrEmpty(availableCounts.ActivationKey))
            {
                availableCounts.ValidationError = "Activation key must not be empty";
                return new ObjectResult(availableCounts);
            }

            var activationKeyEntity = this._activationContext.ActivationKey.Include("CountActivation").FirstOrDefault(ak => ak.Value == availableCounts.ActivationKey);
            if (activationKeyEntity == null)
            {
                availableCounts.ValidationError = "Entered activation key is invalid";
                return new ObjectResult(availableCounts);
            }

            int numNewCounts = 0;
            foreach(var countEntity in activationKeyEntity.CountActivation)
            {
                if(!countEntity.IsActivated && countEntity.ActivationDate == null)
                {
                    numNewCounts += countEntity.Counts;
                }
                countEntity.IsActivated = true;
                countEntity.ActivationDate = DateTime.UtcNow;
            }
            this._activationContext.SaveChanges();

            availableCounts.Counts = numNewCounts;

            return new ObjectResult(availableCounts);
        }
    }
}
