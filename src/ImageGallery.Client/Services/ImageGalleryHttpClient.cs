using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ImageGallery.Client.Services
{
    public class ImageGalleryHttpClient : IImageGalleryHttpClient
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IDiscoveryClient _discoveryClient;
        private HttpClient _httpClient = new HttpClient();

        public ImageGalleryHttpClient(IHttpContextAccessor httpContextAccessor, IDiscoveryClient discoveryClient)
        {
            _httpContextAccessor = httpContextAccessor;
            _discoveryClient = discoveryClient;
        }
        
        public async Task<HttpClient> GetClient()
        {
            string accessToken = string.Empty;

            var context = _httpContextAccessor.HttpContext;

            // get access token
            // accessToken = await context.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);

            // should we renew access and refresh tokens?
            // get expires_at value
            var expires_at = await context.GetTokenAsync("expires_at").ConfigureAwait(false);

            // compare - make sure to use the exact date formats from comparison
            // (UTC)
            if (string.IsNullOrWhiteSpace(expires_at) || 
                (DateTime.Parse(expires_at).AddSeconds(-60).ToUniversalTime() < DateTime.UtcNow))
            {
                accessToken = await RenewTokens().ConfigureAwait(false);
            } 
            else
            {
                // get access token
                accessToken = await context.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            }

            if(!string.IsNullOrWhiteSpace(accessToken))
            {
                // set bearer token
                _httpClient.SetBearerToken(accessToken);
            }

            _httpClient.BaseAddress = new Uri("https://localhost:44325/");
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            return _httpClient;
        }

        private async Task<string> RenewTokens()
        {
            // get the current HttpContext to access the tokens
            var currContext = _httpContextAccessor.HttpContext;

            // get existing refresh_token
            var refreshToken = await currContext.GetTokenAsync("refresh_token").ConfigureAwait(false);

            // get the metadata
            var discoClient = _discoveryClient.GetDiscoveryClient(); // extension method from IdentityModel.Client
            var disco = await discoClient.GetDiscoveryDocumentAsync().ConfigureAwait(false);

            // create a new token client and get new tokens
            var tokenClient = new HttpClient();
            var response = await tokenClient.RequestRefreshTokenAsync(new RefreshTokenRequest
            {
                Address = disco.TokenEndpoint,

                ClientId = "imagegalleryclient",
                ClientSecret = "secret",

                RefreshToken = refreshToken
            }).ConfigureAwait(false);

            if (!response.IsError)
            {
                // update the tokens and expiration value
                var updateTokens = new List<AuthenticationToken>();
                updateTokens.Add(new AuthenticationToken
                {
                    Name = OpenIdConnectParameterNames.IdToken,
                    Value = response.IdentityToken
                });
                updateTokens.Add(new AuthenticationToken
                {
                    Name = OpenIdConnectParameterNames.AccessToken,
                    Value = response.AccessToken
                });
                updateTokens.Add(new AuthenticationToken
                {
                    Name = OpenIdConnectParameterNames.RefreshToken,
                    Value = response.RefreshToken
                });

                var expires_at = DateTime.UtcNow + TimeSpan.FromSeconds(response.ExpiresIn);
                updateTokens.Add(new AuthenticationToken
                {
                    Name = "expires_at",
                    Value = expires_at.ToString("o", CultureInfo.InvariantCulture)
                });

                // we need to update the cookies so that the updated tokens are in the cookies.
                // get authenticate result - contains the current principal and properties
                var currentAuthenticateResult = await currContext.AuthenticateAsync("Cookies").ConfigureAwait(false);

                // store the updated tokens
                currentAuthenticateResult.Properties.StoreTokens(updateTokens);

                // sign in, this call updates our cookie
                await currContext.SignInAsync("Cookies",
                    currentAuthenticateResult.Principal,
                    currentAuthenticateResult.Properties).ConfigureAwait(false);

                return response.AccessToken;
            }
            else
            {
                throw new Exception("Problem while refreshing tokens.", response.Exception);
            }
        }
    }
}

