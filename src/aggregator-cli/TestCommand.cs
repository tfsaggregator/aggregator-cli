using CommandLine;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.DelegatedAuthorization;
using Microsoft.VisualStudio.Services.DelegatedAuthorization.Client;
using Microsoft.VisualStudio.Services.DelegatedAuthorization.WebApi;
using Microsoft.VisualStudio.Services.OAuth;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace aggregator.cli
{
    [Verb("test", HelpText = "Tests something.")]
    class TestCommand : CommandBase
    {
        internal override async Task<int> RunAsync()
        {
            var azure = AzureLogon.Load()?.Logon();
            if (azure == null)
            {
                WriteError($"Must logon.azure first.");
                return 2;
            }
            var vsts = await VstsLogon.Load()?.LogonAsync();
            if (vsts == null)
            {
                WriteError($"Must logon.vsts first.");
                return 2;
            }
            // HERE THE TEST CODE

            var authorizationUri = new Uri("https://app.vssps.visualstudio.com/oauth2/authorize");

            //var acli = vsts.GetClient<Microsoft.VisualStudio.Services.OAuth.Client.OAuthHttpClient>();
            //var aresp = await acli.AuthorizeAsync(ClientAppID, "Assertion", CallbackUrl, Scope, "state", null);

            /*
            //var userNamePasswordAssertion = new UserPasswordCredential(userName, password);
            var userNamePasswordAssertion = new UserCredential(userName);
            var context = new AuthenticationContext("https://login.microsoftonline.com/common");
            var result = await context.AcquireTokenAsync("https://tfsaggregator.visualstudio.com/", ClientAppID, userNamePasswordAssertion);
            */

            string userName = "tfsaggregator@outlook.com";
            string password = "*********************************";

            string ClientAppID = "--------------------";
            string ClientAppSecret = "******************************************************************";
            string Scope = "vso.build vso.code vso.dashboards vso.graph vso.identity vso.notification vso.project vso.release vso.serviceendpoint_manage vso.test vso.wiki_write vso.work_full vso.workitemsearch";
            string CallbackUrl = "urn:ietf:wg:oauth:2.0:oob";

            var oauthEndpoint = new Uri("https://app.vssps.visualstudio.com/oauth2/authorize");

            using (var client = new HttpClient())
            {
                var result = await client.PostAsync(oauthEndpoint, new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", ClientAppID),
                    new KeyValuePair<string, string>("response_type", "Assertion"),
                    new KeyValuePair<string, string>("state", "gibberish"),
                    new KeyValuePair<string, string>("scope", Scope),
                    new KeyValuePair<string, string>("redirect_uri", "https://example.com/test-aggregator-1/callback"),
                }));
                var content = await result.Content.ReadAsStringAsync();
            };

            string token = "";
            var credentials = new VssOAuthAccessTokenCredential(token);

            var connection = new VssConnection(new Uri("https://tfsaggregator.visualstudio.com/"), credentials);
            var pclient = connection.GetClient<ProjectHttpClient>();
            var projects = await pclient.GetProjects();

            return 0;
        }



        class OAuthResult
        {
            public string Token_Type { get; set; }
            public string Scope { get; set; }
            public int Expires_In { get; set; }
            public int Ext_Expires_In { get; set; }
            public int Expires_On { get; set; }
            public int Not_Before { get; set; }
            public Uri Resource { get; set; }
            public string Access_Token { get; set; }
        }
    }
}
