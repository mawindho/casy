namespace OLS.Casy.ActivationServer.Data
{
    using Microsoft.EntityFrameworkCore;
    using System;
    //using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class ActivationContext : DbContext
    {
        public ActivationContext()
            //: base("name=ActivationContext")
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlServer("data source=192.168.110.151;initial catalog=Casy_Activation;User ID=sa; Password=ZcM53211QdV963;MultipleActiveResultSets=True;App=EntityFramework");

        public virtual DbSet<ActivatedMachine> ActivatedMachine { get; set; }
        public virtual DbSet<ActivationKey> ActivationKey { get; set; }
        public virtual DbSet<CountActivation> CountActivation { get; set; }
        public virtual DbSet<Customer> Customer { get; set; }
        public virtual DbSet<ProductAddOn> ProductAddOn { get; set; }
        public virtual DbSet<ProductType> ProductType { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ActivatedMachine>()
                .Property(e => e.MacAdress)
                .IsUnicode(false);

            modelBuilder.Entity<ActivatedMachine>()
                .Property(e => e.SerialNumber)
                .IsUnicode(false);

            modelBuilder.Entity<ActivatedMachine>()
                .Property(e => e.CurrentVersion)
                .IsUnicode(false);

            modelBuilder.Entity<ActivatedMachine>()
                .Property(e => e.ComputerName)
                .IsUnicode(false);

            modelBuilder.Entity<ActivationKey>()
                .Property(e => e.Value)
                .IsUnicode(false);

            modelBuilder.Entity<ActivationKey>()
                .Property(e => e.Responsible)
                .IsUnicode(false);

            modelBuilder.Entity<ActivationKey>()
                .Property(e => e.SerialNumbers)
                .IsUnicode(false);

            modelBuilder.Entity<ActivationKey>()
                .HasMany(e => e.ActivatedMachine)
                .WithOne(e => e.ActivationKey).OnDelete(DeleteBehavior.Restrict);
                //.WithRequired(e => e.ActivationKey)
                //.WillCascadeOnDelete(false);

            modelBuilder.Entity<ActivationKey>()
                .HasMany(e => e.CountActivation)
                .WithOne(e => e.ActivationKey).OnDelete(DeleteBehavior.Restrict);
            //.WithRequired(e => e.ActivationKey)
            //.WillCascadeOnDelete(false);

            modelBuilder.Entity<ActivationKey_ProductAddOns_Mappings>()
                .HasKey(ap => new { ap.ActivationKeyId, ap.ProductAddOnId });

            modelBuilder.Entity<ActivationKey_ProductAddOns_Mappings>()
                .HasOne(e => e.ProductAddOn)
                .WithMany(e => e.ActivationKeyProductAddOns)
                .HasForeignKey(e => e.ProductAddOnId);
            //.Map(m => m.ToTable("ActivationKey_ProductAddOns_Mappings").MapLeftKey("ActivationKeyId").MapRightKey("ProductAddOnId"));

            modelBuilder.Entity<ActivationKey_ProductAddOns_Mappings>()
                .HasOne(e => e.ActivationKey)
                .WithMany(e => e.ActivationKeyProductAddOns)
                .HasForeignKey(e => e.ActivationKeyId);

            modelBuilder.Entity<Customer>()
                .Property(e => e.Name)
                .IsUnicode(false);

            modelBuilder.Entity<Customer>()
                .HasMany(e => e.ActivationKey)
                .WithOne(e => e.Customer).OnDelete(DeleteBehavior.Restrict);
                //.WithRequired(e => e.Customer)
                //.WillCascadeOnDelete(false);

            modelBuilder.Entity<ProductAddOn>()
                .Property(e => e.Type)
                .IsUnicode(false);

            modelBuilder.Entity<ProductType>()
                .Property(e => e.Type)
                .IsUnicode(false);

            modelBuilder.Entity<ProductType>()
                .HasMany(e => e.ActivationKey)
                .WithOne(e => e.ProductType).OnDelete(DeleteBehavior.Restrict);
            //.WithRequired(e => e.ProductType)
            //.WillCascadeOnDelete(false);
        }
    }
}
