using Newtonsoft.Json;
using OLS.Casy.Core.Config.Api;
using OLS.OmniBus.Server.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace OLS.Casy.Com.OmniBus
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IOmniBusDataService))]
    public class OmniBusDataService : IOmniBusDataService, IPartImportsSatisfiedNotification
    {
        private readonly IConfigService _configService;
        private HttpClient _client;

        [ImportingConstructor]
        public OmniBusDataService(IConfigService configService)
        {
            this._configService = configService;
            _client = new HttpClient();
        }

        [ConfigItem("localhost")]
        public string OmniBusServer { get; set; }

        [ConfigItem("49969")]
        public string OmniBusDataServicePort { get; set; }

        public async Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances()
        {
            IEnumerable<WorkflowInstance> workflowInstances = null;
            HttpResponseMessage response = await this._client.GetAsync($"api/WorkflowInstance");
            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                workflowInstances = JsonConvert.DeserializeObject<IEnumerable<WorkflowInstance>>(responseString);
            }
            return workflowInstances;
        }

        public void OnImportsSatisfied()
        {
            this._configService.InitializeByConfiguration(this);
            this._client.BaseAddress = new Uri(string.Format(@"http://{0}:{1}/", OmniBusServer, OmniBusDataServicePort));
            //this._client.BaseAddress = new Uri("http://localhost:50378/");

            this._client.DefaultRequestHeaders.Accept.Clear();
            this._client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }
    }
}
