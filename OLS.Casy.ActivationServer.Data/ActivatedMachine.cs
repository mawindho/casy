namespace OLS.Casy.ActivationServer.Data
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    //using System.Data.Entity.Spatial;

    [Table("ActivatedMachine")]
    public partial class ActivatedMachine
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string MacAdress { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime ActivatedOn { get; set; }

        public int ActivationKeyId { get; set; }

        [StringLength(50)]
        public string SerialNumber { get; set; }

        [StringLength(50)]
        public string CurrentVersion { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime LastUpdatedAt { get; set; }

        [StringLength(50)]
        public string ComputerName { get; set; }

        public virtual ActivationKey ActivationKey { get; set; }
    }
}
