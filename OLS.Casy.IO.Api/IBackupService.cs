﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OLS.Casy.IO.Api
{
    public interface IBackupService
    {
        bool RestoreBackup(string backupPath);
    }
}
