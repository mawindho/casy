namespace OLS.Casy.ActivationServer.Data
{
    public class ActivationKey_ProductAddOns_Mappings
    {
        public int ActivationKeyId { get; set; }
        public ActivationKey ActivationKey { get; set; }
        public int ProductAddOnId { get; set; }
        public ProductAddOn ProductAddOn { get;set; }
    }
}
