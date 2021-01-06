namespace OLS.Casy.ActivationServer.Data
{
    using System;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("CountActivation")]
    public partial class CountActivation
    {
        public int Id { get; set; }

        public int ActivationKeyId { get; set; }

        public int Counts { get; set; }

        public bool IsActivated { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime? ActivationDate { get; set; }

        public virtual ActivationKey ActivationKey { get; set; }
    }
}
