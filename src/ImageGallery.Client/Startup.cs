using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using ImageGallery.Client.Services;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;

namespace ImageGallery.Client
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            // Remove claim mapping for easier read
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc(options => options.EnableEndpointRouting = false);

            // register an IHttpContextAccessor so we can access the current
            // HttpContext in services by injecting it
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // register an IImageGalleryHttpClient
            services.AddScoped<IImageGalleryHttpClient, ImageGalleryHttpClient>();

            // This configuration allows for the flow to be automated.
            // configure authentication middleware on client side application
            // Use Authentication
            // Use/save tokens to encrypted cookies
            // Use IDP server authentication
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = "Cookies";
                options.DefaultChallengeScheme = "oidc";
            }).AddCookie("Cookies")
            .AddOpenIdConnect("oidc", options =>
            {
                options.SignInScheme = "Cookies";
                options.Authority = "https://localhost:44347/"; // idp
                options.ClientId = "imagegalleryclient";
                options.ResponseType = "code id_token"; // hybrid flow
                // options.CallbackPath = new PathString("...") // using default redirect/callback at https://[host]/signin-oidc
                // options.SignoutCallbackPath = new PathString("..."); // using default -> https://[host]/signout-callback-oidc
                options.Scope.Add("openid");    // required by openid
                options.Scope.Add("profile");
                options.SaveTokens = true;
                options.ClientSecret = "secret";
                options.GetClaimsFromUserInfoEndpoint = true;
                options.ClaimActions.Remove("amr"); // remove filter for access method/amr
                options.ClaimActions.DeleteClaim("sid");
                options.ClaimActions.DeleteClaim("idp");
                options.ClaimActions.DeleteClaim("s_hash");
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Shared/Error");
            }

            // It's important that this is before UseMvc, because we want it blocked
            // for unauthenticated users.
            app.UseAuthentication();

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Gallery}/{action=Index}/{id?}");
            });
        }         
    }
}
