using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ImageGallery.Client.ViewModels;
using Newtonsoft.Json;
using ImageGallery.Model;
using System.Net.Http;
using System.IO;
using ImageGallery.Client.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Diagnostics;
using IdentityModel.Client;

namespace ImageGallery.Client.Controllers
{
    [Authorize]
    public class GalleryController : Controller
    {
        private readonly IImageGalleryHttpClient _imageGalleryHttpClient;
        private readonly IDiscoveryClient _discoveryClient;

        public GalleryController(IImageGalleryHttpClient imageGalleryHttpClient,
            IDiscoveryClient discoveryClient)
        {
            _imageGalleryHttpClient = imageGalleryHttpClient;
            _discoveryClient = discoveryClient;
        }

        public async Task<IActionResult> Index()
        {
            await WriteOutIdentityInformation();

            // call the API
            var httpClient = await _imageGalleryHttpClient.GetClient(); 

            var response = await httpClient.GetAsync("api/images").ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var imagesAsString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                var galleryIndexViewModel = new GalleryIndexViewModel(
                    JsonConvert.DeserializeObject<IList<Image>>(imagesAsString).ToList());

                return View(galleryIndexViewModel);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                return RedirectToAction("AccessDenied", "Authorization");

            throw new Exception($"A problem happened while calling the API: {response.ReasonPhrase}");
        }

        public async Task<IActionResult> EditImage(Guid id)
        {
            // call the API
            var httpClient = await _imageGalleryHttpClient.GetClient();

            var response = await httpClient.GetAsync($"api/images/{id}").ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var imageAsString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var deserializedImage = JsonConvert.DeserializeObject<Image>(imageAsString);

                var editImageViewModel = new EditImageViewModel()
                {
                    Id = deserializedImage.Id,
                    Title = deserializedImage.Title
                };
                
                return View(editImageViewModel);
            }
           
            throw new Exception($"A problem happened while calling the API: {response.ReasonPhrase}");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditImage(EditImageViewModel editImageViewModel)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            // create an ImageForUpdate instance
            var imageForUpdate = new ImageForUpdate()
                { Title = editImageViewModel.Title };

            // serialize it
            var serializedImageForUpdate = JsonConvert.SerializeObject(imageForUpdate);

            // call the API
            var httpClient = await _imageGalleryHttpClient.GetClient();

            var response = await httpClient.PutAsync(
                $"api/images/{editImageViewModel.Id}",
                new StringContent(serializedImageForUpdate, System.Text.Encoding.Unicode, "application/json"))
                .ConfigureAwait(false);                        

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }
          
            throw new Exception($"A problem happened while calling the API: {response.ReasonPhrase}");
        }

        public async Task<IActionResult> DeleteImage(Guid id)
        {
            // call the API
            var httpClient = await _imageGalleryHttpClient.GetClient();

            var response = await httpClient.DeleteAsync($"api/images/{id}").ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }
       
            throw new Exception($"A problem happened while calling the API: {response.ReasonPhrase}");
        }
        
        [Authorize(Roles = "PayingUser")]
        public IActionResult AddImage()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "PayingUser")]
        public async Task<IActionResult> AddImage(AddImageViewModel addImageViewModel)
        {   
            if (!ModelState.IsValid)
            {
                return View();
            }

            // create an ImageForCreation instance
            var imageForCreation = new ImageForCreation()
                { Title = addImageViewModel.Title };

            // take the first (only) file in the Files list
            var imageFile = addImageViewModel.Files.First();

            if (imageFile.Length > 0)
            {
                using (var fileStream = imageFile.OpenReadStream())
                using (var ms = new MemoryStream())
                {
                    fileStream.CopyTo(ms);
                    imageForCreation.Bytes = ms.ToArray();                     
                }
            }
            
            // serialize it
            var serializedImageForCreation = JsonConvert.SerializeObject(imageForCreation);

            // call the API
            var httpClient = await _imageGalleryHttpClient.GetClient();

            var response = await httpClient.PostAsync(
                $"api/images",
                new StringContent(serializedImageForCreation, System.Text.Encoding.Unicode, "application/json"))
                .ConfigureAwait(false); 

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }

            throw new Exception($"A problem happened while calling the API: {response.ReasonPhrase}");
        }

        //[Authorize(Roles = "PayingUser")]
        [Authorize(Policy = "CanOrderFrame")]
        public async Task<IActionResult> OrderFrame()
        {
            // using nuget package IdentityModel, it's made by IdentityServer4 people,
            // to get information using the endpoints defined in
            // idphost/.well-known/openid-configuration. In this case
            // we'll use it to get userinfo.

            var client = _discoveryClient.GetDiscoveryClient();

            var discoMetaData = await client.GetDiscoveryDocumentAsync();

            var accessToken = await HttpContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken)
                .ConfigureAwait(false);

            var userInfo = await client.GetUserInfoAsync(new UserInfoRequest
            {
                Address = discoMetaData.UserInfoEndpoint,
                Token = accessToken
            }).ConfigureAwait(false);


            if (userInfo.IsError)
            {
                throw new Exception("Problem accessing the UserInfo endpoint.",
                            userInfo.Exception);
            }

            var address = userInfo.Claims.FirstOrDefault(c => c.Type == "address")?.Value;
            return View(new OrderFrameViewModel(address));
        }
        
        public async Task WriteOutIdentityInformation()
        {
            // get the saved identity token
            var identityToken = await HttpContext
                .GetTokenAsync(OpenIdConnectParameterNames.IdToken).ConfigureAwait(false);

            Debug.WriteLine($"Identity token: {identityToken}");

            // write out the user claims
            foreach(var claim in User.Claims)
            {
                Debug.WriteLine($"Claim type: {claim.Type} - Claim value: {claim.Value}");
            }
        }

        public async Task Logout()
        {
            //var accessToken = await HttpContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken)
            //                            .ConfigureAwait(false);

            //if (!string.IsNullOrWhiteSpace(accessToken))
            //{
            //    var discoClient = _discoveryClient.GetDiscoveryClient(); // extension method from IdentityModel.Client
            //    var disco = await discoClient.GetDiscoveryDocumentAsync().ConfigureAwait(false);

            //    var revocClient = new HttpClient();
            //    var result = await revocClient.RevokeTokenAsync(new TokenRevocationRequest
            //    {
            //        Address = disco.RevocationEndpoint,
            //        ClientId = "imagegalleryclient",
            //        ClientSecret = "secret",
            //        Token = accessToken
            //    }).ConfigureAwait(false);

            //    if(result.IsError)
            //    {
            //        throw new Exception("Problem while revoking access token.", result.Exception);
            //    }
            //}
            

            // Clears the local cookie, "Cookies" must match name from scheme in startup.cs
            await HttpContext.SignOutAsync("Cookies"); // sign out of site
            await HttpContext.SignOutAsync("oidc"); // sign out of idp server
        }
    }
}
