using GrpcService1.Services;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using static System.Net.WebRequestMethods;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.ConfigureHttpsDefaults(options =>
    {        
        options.ClientCertificateMode = ClientCertificateMode.RequireCertificate;

        /*
        options.ServerCertificateSelector = (context, subjectName) =>
        {

            Console.WriteLine("ServerCertificateSelector:" + subjectName);
            if (subjectName == "mydomain.com")
                return new X509Certificate2("ca.pfx");
            else
                return new X509Certificate2("badca.pfx");
        };
        */

        options.ClientCertificateValidation = (cert, chain, policyErrors) =>
        {
            Console.WriteLine("Cert Chain: " + chain?.ChainElements.FirstOrDefault()?.Certificate?.Subject);
            if (policyErrors == SslPolicyErrors.RemoteCertificateNameMismatch && (cert.Subject.StartsWith("CN=mydomain.com") || cert.Subject.StartsWith("CN=chained.mydomain.com")))
                policyErrors = SslPolicyErrors.None;

            if (policyErrors != SslPolicyErrors.None)
            {
                Console.WriteLine("Certificate PolicyErrors: " + policyErrors + " for " + cert.Subject);
                return false;
            }
            else
                return true;
        };
    });
});


builder.Services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme)
    .AddCertificate(options =>
    {
        options.AllowedCertificateTypes = CertificateTypes.All;
        options.RevocationMode = X509RevocationMode.NoCheck;
        /*
        options.ValidateCertificateUse = false;
        options.Events = new CertificateAuthenticationEvents
        {

            OnCertificateValidated = context =>
            {
                Debug.WriteLine("Help");
                Console.WriteLine("Help");
                var claims = new[]
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        context.ClientCertificate.Subject,
                        ClaimValueTypes.String, context.Options.ClaimsIssuer),
                    new Claim(
                        ClaimTypes.Name,
                        context.ClientCertificate.Subject,
                        ClaimValueTypes.String, context.Options.ClaimsIssuer)
                };

                context.Principal = new ClaimsPrincipal(
                    new ClaimsIdentity(claims, context.Scheme.Name));
                context.Success();

                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                int test = Convert.ToInt32("test");
                context.Fail($"Invalid certificate");
                return Task.CompletedTask;
            }
        };
        */
    });

builder.Services.AddGrpc();
builder.Services.AddControllers();

builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true)
    .AddEnvironmentVariables();

var app = builder.Build();

app.UseAuthentication();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCertificateForwarding();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseRouting().UseEndpoints(endpoints =>
{
    endpoints.MapGrpcService<GreeterService>();
    endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
    endpoints.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
});

app.Run();
