namespace OLS.Casy.ActivationServer.Data
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("ActivationKey")]
    public partial class ActivationKey
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public ActivationKey()
        {
            ActivatedMachine = new HashSet<ActivatedMachine>();
            CountActivation = new HashSet<CountActivation>();
            ProductAddOn = new HashSet<ProductAddOn>();
        }

        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Value { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime ValidFrom { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime ValidTo { get; set; }

        public int MaxNumActivations { get; set; }

        public int CustomerId { get; set; }

        //[StringLength(200)]
        public string SerialNumbers { get; set; }

        public int ProductTypeId { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ActivatedMachine> ActivatedMachine { get; set; }

        public virtual Customer Customer { get; set; }

        public virtual ProductType ProductType { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<CountActivation> CountActivation { get; set; }

        [NotMapped] 
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ProductAddOn> ProductAddOn { get; set; }

        [StringLength(200)]
        public string Responsible { get; set; }
    }
}
