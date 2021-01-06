using Microsoft.EntityFrameworkCore;
using OLS.Casy.ActivationServer.Cobra.Models;
//using System.Data.Entity;

namespace OLS.Casy.ActivationServer.Cobra
{
    public class CobraContext : DbContext
    {
        public CobraContext()
            //: base("name=Cobra")
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlServer(@"data source=192.168.110.4;initial catalog=OLSBIO;User ID=dev;Password=reraHefe;MultipleActiveResultSets=True;App=EntityFramework");

        public DbSet<Address> Addresses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Address>().ToTable("ADDRESSES");
            modelBuilder.Entity<Address>().Property(x => x.Id).HasColumnName("ID");
            modelBuilder.Entity<Address>().Property(x => x.Guid).HasColumnName("GUID");
            modelBuilder.Entity<Address>().Property(x => x.Company1).HasColumnName("COMPANY1");
            modelBuilder.Entity<Address>().Property(x => x.Company2).HasColumnName("COMPANY2");
            modelBuilder.Entity<Address>().Property(x => x.Company3).HasColumnName("COMPANY3");
            modelBuilder.Entity<Address>().Property(x => x.Department).HasColumnName("DEPARTMENT0");
            modelBuilder.Entity<Address>().Property(x => x.Salutation).HasColumnName("TOPERSON0");
            modelBuilder.Entity<Address>().Property(x => x.Title).HasColumnName("TITLE0");
            modelBuilder.Entity<Address>().Property(x => x.Firstname).HasColumnName("FIRSTNAME0");
            modelBuilder.Entity<Address>().Property(x => x.Lastname).HasColumnName("LASTNAME0");
            modelBuilder.Entity<Address>().Property(x => x.Position).HasColumnName("POSITION0");
            modelBuilder.Entity<Address>().Property(x => x.Street).HasColumnName("STREET0");
            modelBuilder.Entity<Address>().Property(x => x.Postbox).HasColumnName("POSTBOX0");
            modelBuilder.Entity<Address>().Property(x => x.PostalCode).HasColumnName("ZIP0");
            modelBuilder.Entity<Address>().Property(x => x.City).HasColumnName("CITY0");
            modelBuilder.Entity<Address>().Property(x => x.Country).HasColumnName("STATE0");
            modelBuilder.Entity<Address>().Property(x => x.Phone).HasColumnName("PHONE0");
            modelBuilder.Entity<Address>().Property(x => x.DirectPhone).HasColumnName("DIRECTPHONE0");
            modelBuilder.Entity<Address>().Property(x => x.MobilePhone).HasColumnName("MOBILEPHONE0");
            modelBuilder.Entity<Address>().Property(x => x.Fax).HasColumnName("FAX0");
            modelBuilder.Entity<Address>().Property(x => x.Email).HasColumnName("EMAIL0");
        }
    }
}
