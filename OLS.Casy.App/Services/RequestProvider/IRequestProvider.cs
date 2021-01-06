using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OLS.Casy.App.Services.RequestProvider
{
    public interface IRequestProvider
    {
        Task<TResult> GetAsync<TResult>(string uri, string clientId, string clientSecret);

        Task<TResult> PostAsync<TResult, TData>(string uri, TData data, string clientId, string clientSecret, string header = "");

        Task<TResult> PostAsync<TResult>(string uri, string data, string clientId, string clientSecret);

        Task<TResult> PutAsync<TResult>(string uri, TResult data, string header = "");

        Task DeleteAsync(string uri);
    }
}
