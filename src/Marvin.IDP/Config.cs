using IdentityServer4;
using IdentityServer4.Models;
using IdentityServer4.Test;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Marvin.IDP
{
    public static class Config
    {
        public static List<TestUser> GetUsers()
        {
            return new List<TestUser>
            {
                new TestUser 
                {
                    SubjectId = "d860efca-22d9-47fd-8249-791ba61b07c7",
                    Username = "Frank",
                    Password = "password",

                    Claims = new List<Claim>
                    {
                        new Claim("given_name", "Frank"),
                        new Claim("family_name", "Underwood"),
                        new Claim("address", "1 Main St"),
                        new Claim("role", "FreeUser"),
                        new Claim("subscriptionlevel", "FreeUser"),
                        new Claim("country", "us")

                    }
                },
                new TestUser
                {
                    SubjectId = "b7539694-97e7-4dfe-84da-b4256e1ff5c7",
                    Username = "Claire",
                    Password = "password",

                    Claims = new List<Claim>
                    {
                        new Claim("given_name", "Claire"),
                        new Claim("family_name", "Underwood"),
                        new Claim("address", "36 University St"),
                        new Claim("role", "PayingUser"),
                        new Claim("subscriptionlevel", "PayingUser"),
                        new Claim("country", "mx")
                    }
                }
            };
        }

        // returns scope
        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            return new List<IdentityResource>
            {
                new IdentityResources.OpenId(), // SubjectId above, openid minimum requirement
                new IdentityResources.Profile(), // Claims given_name and family_name
                new IdentityResources.Address(),
                new IdentityResource(
                    "roles",                         // name of the resource
                    "Your role(s)",                  // display name
                    new List<string>() { "role" }),  // List of claims that must be returned when
                                                     // asking for the roles resource.
                new IdentityResource(                   // ABAC
                    "country",
                    "The country your're living in.",
                    new List<string>() { "country" }),
                new IdentityResource(                   // ABAC
                    "subscriptionlevel",
                    "Your subscription level",
                    new List<string>() { "subscriptionlevel" })
            };
        }

        public static IEnumerable<ApiResource> GetApiResources()
        {
            return new List<ApiResource>
            {
                new ApiResource("imagegalleryapi", "Image Gallery API", 
                                    new List<string> { "role" })    // api needs role in access token
                                                                    // so we add it in our IDP config
            };
        }

        public static IEnumerable<Client> GetClients()
        {
            return new List<Client>()
            {
                new Client()
                {
                    ClientName = "Image Gallery",
                    ClientId = "imagegalleryclient",
                    AllowedGrantTypes = GrantTypes.Hybrid,
                    //IdentityTokenLifetime = // default 300 seconds
                    //AuthorizationCodeLifetime = // default 300 seconds
                    AccessTokenLifetime = 120,
                    AllowOfflineAccess = true, // allows us to get new access_token + new refresh_token
                                                // on behave of the user without asking the user to intervene with creds
                    //AbsoluteRefreshTokenLifetime = // default 30 days
                    UpdateAccessTokenClaimsOnRefresh = true, // updates claims if they've been changed.
                    RedirectUris = new List<string>()
                    {
                        "https://localhost:44301/signin-oidc" // tokens delivered to browser via redirection
                    },
                    PostLogoutRedirectUris =
                    {
                        "https://localhost:44301/signout-callback-oidc"
                    },
                    AllowedScopes =     // resources client has access to
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Address,
                        "roles",
                        "imagegalleryapi",
                        "country",
                        "subscriptionlevel"
                    },
                    ClientSecrets =
                    {
                        new Secret("secret".Sha256())
                    }
                }
            };
        }
    }
}
