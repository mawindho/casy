using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Data.SQLite;
using OLS.Casy.Monitoring.Entities;

namespace OLS.Casy.Monitoring.IO
{
    public class MonitoringContext : DbContext
    {
        public DbSet<MonitoringItemEntity> MonitoringItems { get; set; }

        public MonitoringContext()
            : base(new SQLiteConnection() { ConnectionString = @"Data Source=.\monitoring.db;Version=3;Password=th1s1sc4sy;" }, true)
        {
        }

        //protected override void OnConfiguring(DbContextOptionsBuilder options)
            //=> options.UseSqlite(@"Data Source=.\monitoring.db;Version=3;Password=th1s1sc4sy;");

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //foreach (IMutableEntityType entity in modelBuilder.Model.GetEntityTypes())
            //{
            //entity.Relational().TableName = entity.DisplayName();
            //}
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }
    }
}
