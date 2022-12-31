using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Http3GrpcUnitTest
{
    public class UnitTestHelpers
    {
        public static HttpClient CreateHttpClient(Version version, string certificate, string password = "")
        {
            return new HttpClient(CreateHttpHandler(certificate, password))
            {
                DefaultRequestVersion = version,
                DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact
            };
        }

        public static async Task<string> Ping(HttpClient httpClient, string url)
        {
            var result = await httpClient.GetAsync(url + "/api/greet/ping");
            return await result.Content.ReadAsStringAsync();
        }

        public static HttpClientHandler CreateHttpHandler(string certificate, string password = "")
        {
            var handler = new HttpClientHandler();
            var cert = new X509Certificate2(certificate, password);
            handler.ClientCertificates.Add(cert);
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            handler.PreAuthenticate = false;

            return handler;
        }
    }
}
