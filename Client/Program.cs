using System;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel.Client;

namespace Client
{
    class Program
    {
        private static async Task Main(string[] args)
        {
            var client = new HttpClient();

            var disco = await client.GetDiscoveryDocumentAsync(new DiscoveryDocumentRequest
            {
                Address = "http://localhost:5000",
                Policy = new DiscoveryPolicy
                {
                    RequireHttps = false
                }
            });

            if (disco.IsError)
            {
                Console.WriteLine(disco.Error);
                return;
            }

            Console.WriteLine(disco.Json);
            Console.WriteLine("\n\n");

            // request token
            var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = disco.TokenEndpoint,
                ClientId = "client",
                ClientSecret = "secret",

                Scope = "openid"
            });

            if (tokenResponse.IsError)
            {
                Console.WriteLine(tokenResponse.Error);
                return;
            }

            Console.WriteLine(tokenResponse.Json);
            Console.WriteLine("\n\n");

            var apiClient = new HttpClient();
            apiClient.SetBearerToken(tokenResponse.AccessToken);
            
            var response = await apiClient.GetAsync("http://localhost:6000/identity");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(response.StatusCode);
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine(content);
            }
        }
    }
}
