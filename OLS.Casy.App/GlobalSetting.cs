using System;

namespace OLS.Casy.App
{
    public class GlobalSetting
    {
        public const string DefaultCasyEndpoint = "http://45.83.106.120:8536";

        private string _casyEndpointBase;

        public GlobalSetting()
        {
            AuthToken = "th1s1sc4sy";

            CasyEndpointBase = DefaultCasyEndpoint;
        }

        public string CasyEndpointBase
        {
            get => _casyEndpointBase;
            set
            {
                _casyEndpointBase = value;
                UpdateCasyEndpoint(_casyEndpointBase);
            }
        }

        public static GlobalSetting Instance { get; } = new GlobalSetting();

        public string AuthToken { get; set; }
        public string CasyEndpoint { get; set; }

        private void UpdateCasyEndpoint(string endpoint)
        {
            CasyEndpoint = $"{endpoint}";
        }

        private string ExtractBaseUri(string endpoint)
        {
            var uri = new Uri(endpoint);
            var baseUri = uri.GetLeftPart(UriPartial.Authority);

            return baseUri;
        }
    }
}
