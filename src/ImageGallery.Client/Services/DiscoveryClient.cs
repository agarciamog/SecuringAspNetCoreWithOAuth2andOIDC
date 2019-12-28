using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.AspNetCore.Http;

namespace ImageGallery.Client.Services
{
    public class DiscoveryClient : IDiscoveryClient
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private HttpClient client = new HttpClient();

        public DiscoveryClient(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public HttpClient GetDiscoveryClient()
        {
            client.BaseAddress = new Uri("https://localhost:44347/");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            return client;
        }
    }
}
