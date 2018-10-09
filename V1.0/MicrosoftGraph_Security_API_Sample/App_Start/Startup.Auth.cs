﻿/* 
*  Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. 
*  See LICENSE in the source repository root for complete license information. 
*/

using System.Web;
using System.Web.Mvc;
using Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using System.Configuration;
using System.Threading.Tasks;
using MicrosoftGraph_Security_API_Sample.TokenStorage;
using System.IdentityModel.Tokens;
using System.IdentityModel.Claims;
using Microsoft.Identity.Client;
using MicrosoftGraph_Security_API_Sample.Controllers;
using System.Collections.Generic;

namespace MicrosoftGraph_Security_API_Sample
{
    public partial class Startup 
    {

        // The appId is used by the application to uniquely identify itself to Azure AD.
        // The appSecret is the application's password.
        // The redirectUri is where users are redirected after sign in and consent.
        // The graphScopes are the Microsoft Graph permission scopes that are used by this sample: User.Read
        private static string appId = ConfigurationManager.AppSettings["ida:AppId"];
        private static string appSecret = ConfigurationManager.AppSettings["ida:AppSecret"];
        private static string redirectUri = ConfigurationManager.AppSettings["ida:RedirectUri"];
        private static string graphScopes = ConfigurationManager.AppSettings["ida:GraphScopes"];

        public static string UserScopes = string.Empty;

        public void ConfigureAuth(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectAuthenticationOptions
                {

                    // The `Authority` represents the Microsoft v2.0 authentication and authorization service.
                    // The `Scope` describes the permissions that your app will need. See https://azure.microsoft.com/documentation/articles/active-directory-v2-scopes/                    
                    ClientId = appId,
                    Authority = "https://login.microsoftonline.com/common/v2.0",
                    PostLogoutRedirectUri = redirectUri,
                    RedirectUri = redirectUri,
                    Scope = "openid email profile offline_access " + graphScopes,
                    TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false
                    },
                    Notifications = new OpenIdConnectAuthenticationNotifications
                    {
                        AuthorizationCodeReceived = async (context) =>
                        {
                            var code = context.Code;
                            string signedInUserID = context.AuthenticationTicket.Identity.FindFirst(ClaimTypes.NameIdentifier).Value;

                            TokenCache userTokenCache = new SessionTokenCache(signedInUserID,
                                context.OwinContext.Environment["System.Web.HttpContextBase"] as HttpContextBase).GetMsalCacheInstance();
                            ConfidentialClientApplication cca = new ConfidentialClientApplication(
                                appId, 
                                redirectUri,
                                new ClientCredential(appSecret),
                                userTokenCache,
                                null);
                            string[] scopes = graphScopes.Split(new char[] { ' ' });

                            AuthenticationResult result = await cca.AcquireTokenByAuthorizationCodeAsync(code, scopes); 
                            UserScopes = string.Join(" ", result.Scopes);
                        },
             
                    }
                });
        }
    }
}