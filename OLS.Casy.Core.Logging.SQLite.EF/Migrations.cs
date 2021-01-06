namespace OLS.Casy.Core.Logging.SQLite.EF
{
    public static class Migrations
    {
        private const long RequiredDatabaseVersion = 1;

        internal static bool CheckForMigration(LogContext logContext)
        {
            //var connection = logContext.Database.GetDbConnection();
            var connection = logContext.Database.Connection;
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA user_version;";
                var currentDbVersion = (long) command.ExecuteScalar();

                return currentDbVersion < RequiredDatabaseVersion;
            }
        }

        internal static void DoMigration(LogContext logContext)
        {
            long currentDbVersion;

            //var connection = logContext.Database.GetDbConnection();
            var connection = logContext.Database.Connection;
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA user_version;";
                currentDbVersion = (long)command.ExecuteScalar();
            }

            if (currentDbVersion >= RequiredDatabaseVersion) return;

            switch (currentDbVersion)
            {
                case 0:
                    MigrateTo1(logContext);
                    break;
            }
        }

        private static void MigrateTo1(LogContext logContext)
        {
            logContext.Database.ExecuteSqlCommand(
                "ALTER TABLE Log ADD COLUMN Category INTEGER DEFAULT 0");

            logContext.Database.ExecuteSqlCommand("PRAGMA user_version = 1");

            DoMigration(logContext);
        }
    }
}
