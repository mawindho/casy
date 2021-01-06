using Microsoft.EntityFrameworkCore;

namespace OLS.Casy.ActivationServer.Data
{


    public class ActivationContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"data source=192.168.110.151;initial catalog=Casy_Activation;integrated security=True;MultipleActiveResultSets=True;App=EntityFramework");
        }

        public virtual DbSet<ActivatedMachine> ActivatedMachine { get; set; }
        public virtual DbSet<ActivationKey> ActivationKey { get; set; }
        public virtual DbSet<CountActivation> CountActivation { get; set; }
        public virtual DbSet<Customer> Customer { get; set; }
        public virtual DbSet<ProductAddOn> ProductAddOn { get; set; }
        public virtual DbSet<ProductType> ProductType { get; set; }
        public virtual DbSet<ActivationKeyAddOnMapping> ActivationKeyAddOnMapping { get; set; }
        public virtual DbSet<Settings> Settings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Settings>(entity =>
            {
                entity.Property(e => e.SettingsKey)
                    .IsUnicode(false);

                entity.Property(e => e.SettingsValue)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<ActivatedMachine>(entity =>
            {
                entity.Property(e => e.MacAdress)
                    .IsUnicode(false);

                entity.Property(e => e.SerialNumber)
                    .IsUnicode(false);

                entity.Property(e => e.CurrentVersion)
                    .IsUnicode(false);

                entity.Property(e => e.ComputerName)
                    .IsUnicode(false);

                entity.HasOne(e => e.ActivationKey)
                    .WithMany(p => p.ActivatedMachine)
                    .HasForeignKey(d => d.ActivationKeyId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_ActivatedMachine_ActivationKey");
            });

            modelBuilder.Entity<ActivationKey>(entity =>
            {
                entity.Property(e => e.Value)
                    .IsUnicode(false);

                entity.Property(e => e.SerialNumbers)
                    .IsUnicode(false);

                entity.HasOne(e => e.Customer)
                    .WithMany(p => p.ActivationKey)
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_ActivationKey_Customer");

                entity.HasOne(e => e.ProductType)
                    .WithMany(p => p.ActivationKey)
                    .HasForeignKey(e => e.ProductTypeId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_ActivationKey_ProductType");
            });

            modelBuilder.Entity<CountActivation>(entity =>
            {
                entity.HasOne(e => e.ActivationKey)
                    .WithMany(p => p.CountActivation)
                    .HasForeignKey(d => d.ActivationKeyId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_CountActivation_ActivationKey");
            });

            modelBuilder.Entity<ProductAddOn>(entity =>
            {
                entity.Property(e => e.Type)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<ActivationKeyAddOnMapping>(entity =>
            {
                entity.HasKey(r => new {r.ActivationKeyId, r.ProductAddOnId});
            });

            modelBuilder.Entity<Customer>(entity =>
            {
                entity.Property(e => e.Name)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<ProductType>(entity =>
            {
                entity.Property(e => e.Type)
                    .IsUnicode(false);
            });

//            modelBuilder.Entity<ActivationKey>()
//                .HasMany(e => e.ActivatedMachine)
//                .WithRequired(e => e.ActivationKey)
//                .WillCascadeOnDelete(false);

            //            modelBuilder.Entity<ActivationKey>()
            //                .HasMany(e => e.CountActivation)
            //                .WithRequired(e => e.ActivationKey)
            //                .WillCascadeOnDelete(false);

//            modelBuilder.Entity<ActivationKey>()
//                .HasMany(e => e.ProductAddOn)
//                .WithMany(e => e.ActivationKey)
//                .Map(m => m.ToTable("ActivationKey_ProductAddOns_Mappings").MapLeftKey("ActivationKeyId").MapRightKey("ProductAddOnId"));

//            modelBuilder.Entity<Customer>()
//                .HasMany(e => e.ActivationKey)
//                .WithRequired(e => e.Customer)
//                .WillCascadeOnDelete(false);

           
//            modelBuilder.Entity<ProductType>()
//                .HasMany(e => e.ActivationKey)
//                .WithRequired(e => e.ProductType)
//                .WillCascadeOnDelete(false);
        }
    }
}
