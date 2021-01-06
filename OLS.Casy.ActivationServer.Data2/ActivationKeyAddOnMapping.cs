using System.ComponentModel.DataAnnotations.Schema;

namespace OLS.Casy.ActivationServer.Data
{
    [Table("ActivationKey_ProductAddOns_Mappings")]
    public class ActivationKeyAddOnMapping
    {
        public int ActivationKeyId { get; set; }
        public ActivationKey ActivationKey { get; set; }
        public int ProductAddOnId { get; set; }
        public ProductAddOn ProductAddOn { get; set; }
    }
}
