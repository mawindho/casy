using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Data.SQLite;
using OLS.Casy.Core.Logging.SQLite.EF.Entities;

namespace OLS.Casy.Core.Logging.SQLite.EF
{
    public class LogContext : DbContext
    {
        public DbSet<LogEntity> Logs { get; set; }

        public LogContext()
            : base(new SQLiteConnection() { ConnectionString = @"Data Source=.\log.db;Version=3;Password=th1s1sc4sy;" }, true)
        {
        }

        //protected override void OnConfiguring(DbContextOptionsBuilder options)
        //{
            //var connection = new SQLiteConnection(@"Data Source=.\log.db;Version=3;"); //Version=3;Password=th1s1sc4sy;
            //connection.Open();

            //using (var command = connection.CreateCommand())
            //{
            //command.CommandText = "PRAGMA key = 'th1s1sc4sy';";
            //command.ExecuteNonQuery();
            //}

            //var command = connection.CreateCommand();
            //command.CommandText = "SELECT quote($password);";
            //command.Parameters.AddWithValue("$password", "th1s1sc4sy");
            //var quotedPassword = (string)command.ExecuteScalar();

            //command.CommandText = "PRAGMA key = " + quotedPassword;
            //command.Parameters.Clear();
            //command.ExecuteNonQuery();

            //options.UseSqlite(connection);
        //}


        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }

        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //base.OnModelCreating(modelBuilder);

        //foreach (IMutableEntityType entity in modelBuilder.Model.GetEntityTypes())
        //{
        //entity.Relational().TableName = entity.DisplayName();
        //}
        //}
    }
}
