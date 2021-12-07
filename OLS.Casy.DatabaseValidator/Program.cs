using OLS.Casy.Core;
using OLS.Casy.Core.Api;
using OLS.Casy.Core.Config;
using OLS.Casy.Core.Config.Api;
using OLS.Casy.Core.Logging.Api;
using OLS.Casy.Core.Logging.SQLite.EF;
using OLS.Casy.IO;
using OLS.Casy.IO.Api;
using OLS.Casy.IO.SQLite.Entities;
using OLS.Casy.IO.SQLite.Standard;
using OLS.Casy.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OLS.Casy.DatabaseValidator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Please ensure the CASY software has been shut down!");
            Console.WriteLine("Press <ENTER> key to continue.");
            Console.ReadLine();
            Console.WriteLine("Checking CASY database ...");

            var environmentService = new EnvironmentService();
            var casyContext = new CasyContext(environmentService, null, null);

            var result = new List<MeasureSetup>();

            var measureSetupEntities =
                casyContext.MeasureSetups.Where(ms => ms.IsTemplate && ms.IsReadOnly).ToList();

            if(!measureSetupEntities.Any(x => x.Name == "Background" && x.CapillarySize == 45))
            {
                Console.WriteLine("Background Template 45 missing. Generating template ...");
                CreateMeasureSetup(casyContext, "Background", 45, 20, Models.Enums.Volumes.TwoHundred);
            }

            if (!measureSetupEntities.Any(x => x.Name == "Background" && x.CapillarySize == 60))
            {
                Console.WriteLine("Background Template 60 missing. Generating template ...");
                CreateMeasureSetup(casyContext, "Background", 60, 30, Models.Enums.Volumes.TwoHundred);
            }

            if (!measureSetupEntities.Any(x => x.Name == "Background" && x.CapillarySize == 150))
            {
                Console.WriteLine("Background Template 150 missing. Generating template ...");
                CreateMeasureSetup(casyContext, "Background", 150, 50, Models.Enums.Volumes.FourHundred);
            }

            if (!measureSetupEntities.Any(x => x.Name == "Test Pattern" && x.CapillarySize == 45))
            {
                Console.WriteLine("Test Pattern Template 45 missing. Generating template ...");
                CreateMeasureSetup(casyContext, "Test Pattern", 45, 30, Models.Enums.Volumes.FourHundred);
            }

            if (!measureSetupEntities.Any(x => x.Name == "Test Pattern" && x.CapillarySize == 60))
            {
                Console.WriteLine("Test Pattern Template 60 missing. Generating template ...");
                CreateMeasureSetup(casyContext, "Test Pattern", 60, 40, Models.Enums.Volumes.FourHundred);
            }

            if (!measureSetupEntities.Any(x => x.Name == "Test Pattern" && x.CapillarySize == 150))
            {
                Console.WriteLine("Test Pattern Template 150 missing. Generating template ...");
                CreateMeasureSetup(casyContext, "Test Pattern", 150, 120, Models.Enums.Volumes.FourHundred);
            }

            Console.WriteLine("Done!");
            Console.WriteLine("Press any key to close the application ...");
            Console.ReadKey();
        }

        private static void CreateMeasureSetup(CasyContext context, string name, int capillary, int toDiameter, Models.Enums.Volumes volume)
        {
            var measureSetupEntity = new MeasureSetupEntity
            {
                AggregationCalculationMode = Models.Enums.AggregationCalculationModes.Off,
                CapillarySize = capillary,
                DilutionFactor = name == "Background" ? 1d : 200d,
                FromDiameter = 0,
                IsDeviationControlEnabled = false,
                IsSmoothing = false,
                IsTemplate = true,
                ManualAggrgationCalculationFactor = 0d,
                MeasureMode = Models.Enums.MeasureModes.MultipleCursor,
                Name = name,
                Repeats = name == "Background" ? 1 : 3,
                ScalingMaxRange = 0,
                ScalingMode = Models.Enums.ScalingModes.Auto,
                SmoothingFactor = 0,
                ToDiameter = toDiameter,
                UnitMode = Models.Enums.UnitModes.Counts,
                Volume = volume,
                VolumeCorrectionFactor = 0,
                IsReadOnly = true,
                AutoSaveName = string.Empty,
                IsAutoSave = false,
                DefaultExperiment = string.Empty,
                DefaultGroup = string.Empty,
                IsAutoComment = false,
                ChannelCount = 1024,
                HasSubpopulations = false,
                CreatedAt = DateTime.Now.ToString(),
                CreatedBy = "setup"
            };

            if (name == "Test Pattern")
            {
                double minLimit1 = 0d, maxLimit1 = 0d, minLimit2 = 0d, maxLimit2 = 0d;
                switch(capillary)
                {
                    case 45:
                        minLimit1 = 6.02;
                        maxLimit1 = 12d;
                        minLimit2 = 12.03;
                        maxLimit2 = 30d;
                        break;
                    case 60:
                        minLimit1 = 8.03;
                        maxLimit1 = 16d;
                        minLimit2 = 16.04;
                        maxLimit2 = 40d;
                        break;
                    case 150:
                        minLimit1 = 24.08;
                        maxLimit1 = 47.99;
                        minLimit2 = 48.11;
                        maxLimit2 = 120d;
                        break;
                }

                measureSetupEntity.CursorEntities.Add(new CursorEntity()
                {
                    Color = "#FFB21B1C",
                    CreatedAt = DateTime.Now.ToString(),
                    CreatedBy = "setup",
                    MinLimit = minLimit1,
                    MaxLimit = maxLimit1,
                    Name = "Normalization Range"
                });

                measureSetupEntity.CursorEntities.Add(new CursorEntity()
                {
                    Color = "#FF1CB269",
                    CreatedAt = DateTime.Now.ToString(),
                    CreatedBy = "setup",
                    MinLimit = minLimit2,
                    MaxLimit = maxLimit2,
                    Name = "Evaluation Range"
                });
            }

            context.MeasureSetups.Add(measureSetupEntity);
            context.SaveChanges(true);
            
        }
    }
}
