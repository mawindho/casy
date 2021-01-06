using log4net.Appender;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OLS.Casy.Core.Logging.SQLite.EF
{
    public class CustomAdoNetAppender : AdoNetAppender
    {
        private string _connectionString;
        protected override string ResolveConnectionString(out string connectionStringContext)
        {
            if (string.IsNullOrEmpty(_connectionString))
            {
                _connectionString = base.ResolveConnectionString(out connectionStringContext);
                _connectionString = _connectionString.Replace("PASSWORD", "th1s1sc4sy");
            }

            connectionStringContext = _connectionString;
            return connectionStringContext;
        }
    }
}
