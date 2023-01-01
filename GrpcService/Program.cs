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

            // attempt to fix http3 issues but gRPC still fails
            /*
             *  Error Message:
   Grpc.Core.RpcException : Status(StatusCode="Unavailable", Detail="Error starting gRPC call. HttpRequestException: Connection refused (mydomain.com:5003) SocketException: Connection refused", DebugException="System.Net.Http.HttpRequestException: Connection refused (mydomain.com:5003)
 ---> System.Net.Sockets.SocketException (111): Connection refused
   at System.Net.Sockets.Socket.AwaitableSocketAsyncEventArgs.ThrowException(SocketError error, CancellationToken cancellationToken)
   at System.Net.Sockets.Socket.AwaitableSocketAsyncEventArgs.System.Threading.Tasks.Sources.IValueTaskSource.GetResult(Int16 token)
   at System.Net.Sockets.Socket.<ConnectAsync>g__WaitForConnectWithCancellation|281_0(AwaitableSocketAsyncEventArgs saea, ValueTask connectTask, CancellationToken cancellationToken)
   at System.Net.Http.HttpConnectionPool.ConnectToTcpHostAsync(String host, Int32 port, HttpRequestMessage initialRequest, Boolean async, CancellationToken cancellationToken)
   --- End of inner exception stack trace ---
   at System.Net.Http.HttpConnectionPool.ConnectToTcpHostAsync(String host, Int32 port, HttpRequestMessage initialRequest, Boolean async, CancellationToken cancellationToken)
   at System.Net.Http.HttpConnectionPool.ConnectAsync(HttpRequestMessage request, Boolean async, CancellationToken cancellationToken)
   at System.Net.Http.HttpConnectionPool.AddHttp2ConnectionAsync(QueueItem queueItem)
   at System.Threading.Tasks.TaskCompletionSourceWithCancellation`1.WaitWithCancellationAsync(CancellationToken cancellationToken)
   at System.Net.Http.HttpConnectionPool.HttpConnectionWaiter`1.WaitForConnectionAsync(Boolean async, CancellationToken requestCancellationToken)
   at System.Net.Http.HttpConnectionPool.SendWithVersionDetectionAndRetryAsync(HttpRequestMessage request, Boolean async, Boolean doRequestAuth, CancellationToken cancellationToken)
   at System.Net.Http.RedirectHandler.SendAsync(HttpRequestMessage request, Boolean async, CancellationToken cancellationToken)
   at System.Net.Http.HttpClient.<SendAsync>g__Core|83_0(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationTokenSource cts, Boolean disposeCts, CancellationTokenSource pendingRequestsCts, CancellationToken originalCancellationToken)
   at Grpc.Net.Client.Internal.GrpcCall`2.RunCall(HttpRequestMessage request, Nullable`1 timeout)")
  Stack Trace:
     at Http3GrpcUnitTest.UnitTest1.GrpcHttpVersion30SelfSignedTest() in /app/Http3GrpcUnitTest/UnitTest1.cs:line 36
--- End of stack trace from previous location ---*/

            if (policyErrors == SslPolicyErrors.RemoteCertificateNameMismatch && (cert.Subject.StartsWith("CN=mydomain.com") || cert.Subject.StartsWith("CN=chained.mydomain.com")))
                policyErrors = SslPolicyErrors.None;

            if (policyErrors != SslPolicyErrors.None)
            {
                Console.WriteLine("Certificate PolicyErrors: " + policyErrors + " for " + cert.);
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
