namespace OLS.Casy.App.Models
{
    public class CasyModel
    {
        public string SerialNumber { get; set; }
        public string IpAddress { get; set; }
        public string DeviceName { get; set; }

        public string DisplayName => $"{DeviceName} {SerialNumber} (IP: {IpAddress})";
    }
}
