using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;

namespace ImageGallery.Client.Services
{
    public interface IDiscoveryClient
    {
        HttpClient GetDiscoveryClient();
    }
}
