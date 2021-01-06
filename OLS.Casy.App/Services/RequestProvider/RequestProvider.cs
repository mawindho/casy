using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using OLS.Casy.App.Exceptions;
using Polly;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace OLS.Casy.App.Services.RequestProvider
{
    public class RequestProvider : IRequestProvider
    {
        private readonly JsonSerializerSettings _serializerSettings;

        public RequestProvider()
        {
            _serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                NullValueHandling = NullValueHandling.Ignore
            };
            _serializerSettings.Converters.Add(new StringEnumConverter());
            //_serializerSettings.Converters.Add(new FunctionsConverter());
            //_serializerSettings.Converters.Add(new ActionTypeParametersConverter());
            //_serializerSettings.Converters.Add(new WorkbookInfoConverter());
        }

        public async Task<TResult> GetAsync<TResult>(string uri, string clientId, string clientSecret)
        {
            var httpClient = CreateHttpClient();

            if (!string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(clientSecret))
            {
                AddBasicAuthenticationHeader(httpClient, clientId, clientSecret);
            }

            var response = await Policy.Handle<HttpRequestException>()
                .OrResult<HttpResponseMessage>(msg => !msg.IsSuccessStatusCode)
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (ex, timeSpan) =>
                    {
                        Console.WriteLine($"Something went wrong: {ex.Result?.StatusCode}, retrying...");
                    }).ExecuteAsync(async () => await httpClient.GetAsync(uri));


            //var response = await httpClient.GetAsync(uri);

            await HandleResponse(response);
            var serialized = await response.Content.ReadAsStringAsync();

            var result = await Task.Run(() =>
                JsonConvert.DeserializeObject<TResult>(serialized, _serializerSettings));

            return result;
        }

        public async Task<TResult> PostAsync<TResult, TData>(string uri, TData data,
            string clientId, string clientSecret, string header = "")
        {
            var httpClient = CreateHttpClient();

            if (!string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(clientSecret))
            {
                AddBasicAuthenticationHeader(httpClient, clientId, clientSecret);
            }

            if (!string.IsNullOrEmpty(header))
            {
                AddHeaderParameter(httpClient, header);
            }

            var content = new StringContent(JsonConvert.SerializeObject(data));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await Policy.Handle<HttpRequestException>()
                //.OrResult<HttpResponseMessage>(msg => !msg.IsSuccessStatusCode)
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (ex, timeSpan) =>
                    {
                        Console.WriteLine($"Something went wrong: {ex.Message}, retrying...");
                    }).ExecuteAsync(async () => await httpClient.PostAsync(uri, content));

            await HandleResponse(response);
            var serialized = await response.Content.ReadAsStringAsync();

            var result = await Task.Run(() =>
                JsonConvert.DeserializeObject<TResult>(serialized, _serializerSettings));

            return result;
        }

        public async Task<TResult> PostAsync<TResult>(string uri, string data, string clientId, string clientSecret)
        {
            HttpClient httpClient = CreateHttpClient();

            if (!string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(clientSecret))
            {
                AddBasicAuthenticationHeader(httpClient, clientId, clientSecret);
            }

            var content = new StringContent(data);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            var response = await Policy.Handle<HttpRequestException>()
                .OrResult<HttpResponseMessage>(msg => !msg.IsSuccessStatusCode)
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (ex, timeSpan) =>
                    {
                        Console.WriteLine($"Something went wrong: {ex.Result?.StatusCode}, retrying...");
                    }).ExecuteAsync(async () => await httpClient.PostAsync(uri, content));
            //var response = await httpClient.PostAsync(uri, content);

            await HandleResponse(response);
            var serialized = await response.Content.ReadAsStringAsync();

            var result = await Task.Run(() =>
                JsonConvert.DeserializeObject<TResult>(serialized, _serializerSettings));

            return result;
        }

        public async Task<TResult> PutAsync<TResult>(string uri, TResult data, string header = "")
        {
            var httpClient = CreateHttpClient();

            if (!string.IsNullOrEmpty(header))
            {
                AddHeaderParameter(httpClient, header);
            }

            var content = new StringContent(JsonConvert.SerializeObject(data));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await Policy.Handle<HttpRequestException>()
                .OrResult<HttpResponseMessage>(msg => !msg.IsSuccessStatusCode)
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (ex, timeSpan) =>
                    {
                        Console.WriteLine($"Something went wrong: {ex.Result?.StatusCode}, retrying...");
                    }).ExecuteAsync(async () => await httpClient.PutAsync(uri, content));
            //var response = );

            await HandleResponse(response);
            var serialized = await response.Content.ReadAsStringAsync();

            var result = await Task.Run(() =>
                JsonConvert.DeserializeObject<TResult>(serialized, _serializerSettings));

            return result;
        }

        public async Task DeleteAsync(string uri)
        {
            var httpClient = CreateHttpClient();

            await Policy.Handle<HttpRequestException>().OrResult<HttpResponseMessage>(msg => !msg.IsSuccessStatusCode)
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (ex, timeSpan) =>
                    {
                        Console.WriteLine($"Something went wrong: {ex.Result?.StatusCode}, retrying...");
                    }).ExecuteAsync(async () => await httpClient.DeleteAsync(uri));
        }


        private HttpClient CreateHttpClient()
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            //if (!string.IsNullOrEmpty(token))
            //{
                //httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            //}

            return httpClient;
        }

        private void AddHeaderParameter(HttpClient httpClient, string parameter)
        {
            if (httpClient == null)
                return;

            if (string.IsNullOrEmpty(parameter))
                return;

            httpClient.DefaultRequestHeaders.Add(parameter, Guid.NewGuid().ToString());
        }

        private void AddBasicAuthenticationHeader(HttpClient httpClient, string clientId, string clientSecret)
        {
            if (httpClient == null)
                return;

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
                return;

            httpClient.DefaultRequestHeaders.Authorization = new BasicAuthenticationHeaderValue(clientId, clientSecret);
        }


        private async Task HandleResponse(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.Forbidden ||
                    response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new ServiceAuthenticationException();
                }

                var content = await response.Content.ReadAsStringAsync();

                throw new HttpRequestExceptionEx(response.StatusCode, content);
            }
        }
    }
}
