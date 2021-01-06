using OLS.Casy.Core;
using OLS.Casy.IO.SQLite.EF;
using OLS.Casy.IO.SQLite.Entities;
using OLS.Casy.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace OLS.Casy.Authorization.ActiveDirectory.SetGroups
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Enter name of CASY User AD-Group [Default: CASY-User]:");
            var casyUserGroup = Console.ReadLine();
            if(string.IsNullOrEmpty(casyUserGroup))
            {
                casyUserGroup = "CASY-User";
            }

            Console.WriteLine("Enter name of CASY Operator AD-Group [Default: CASY-Operator]:");
            var casyOperatorGroup = Console.ReadLine();
            if (string.IsNullOrEmpty(casyOperatorGroup))
            {
                casyOperatorGroup = "CASY-Operator";
            }

            Console.WriteLine("Enter name of CASY Supervisor AD-Group [Default: CASY-Supervisor]:");
            var casySupervisorGroup = Console.ReadLine();
            if (string.IsNullOrEmpty(casySupervisorGroup))
            {
                casySupervisorGroup = "CASY-Supervisor";
            }

            var environmentService = new EnvironmentService();
            var casyContext = new CasyContext2(environmentService, null, null);
            
            SaveSetting(casyContext, "AdGroupUser", casyUserGroup);
            SaveSetting(casyContext, "AdGroupOperator", casyOperatorGroup);
            SaveSetting(casyContext, "AdGroupSupervisor", casySupervisorGroup);
        }

        public static Dictionary<string, Setting> GetSettings(CasyContext2 casyContext)
        {
            var result = new Dictionary<string, Setting>();

            var settingsEntities = casyContext.Settings.ToList();

            foreach (var settingsEntity in settingsEntities)
            {
                result.Add(settingsEntity.Key, new Setting { Id = settingsEntity.Id, Value = settingsEntity.Value, Key = settingsEntity.Key, BlobValue = settingsEntity.BlobValue });
            }
            return result;
        }

        public static void SaveSetting(CasyContext2 casyContext, string key, string value)
        {
            var settingEntity = casyContext.Settings.FirstOrDefault(setting => setting.Key == key);

            if (settingEntity == null)
            {
                settingEntity = new SettingsEntity()
                {
                    Key = key,
                    Value = value
                };

                casyContext.Settings.Add(settingEntity);
                casyContext.SaveChanges();
            }
            else
            {
                settingEntity.Value = value;
                casyContext.SaveChanges();
            }
        }
    }
}
