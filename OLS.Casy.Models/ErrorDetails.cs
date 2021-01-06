using OLS.Casy.Models.Enums;

namespace OLS.Casy.Models
{
    public class ErrorDetails
    {
        public int ErrorDetailsId { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorNumber { get; set; }
        public string DeviceErrorName { get; set; }
        public ErrorCategory ErrorCategory { get; set; }
        public string Description { get; set; }
        public string Notice { get; set; }
        public string Information { get; set; }
    }
}
