using Grpc.Net.Client;
using GrpcService1;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Channels;

namespace Http3GrpcUnitTest
{
    public class UnitTest1
    {
        public const string c_SELF_SIGNED_URL = "https://mydomain.com:5001";
        public const string c_CHAINED_URL = "https://chained.mydomain.com:5002";
        public const string c_HTTP3_SELF_SIGNED_URL = "https://mydomain.com:5003";
        public const string c_HTTP3_CHAINED_URL = "https://chained.mydomain.com:5004";

        #region Self Signed Tests
        [Fact]
        public async Task HttpVersion20SelfSignedTest()
        {
            var client = UnitTestHelpers.CreateHttpClient(HttpVersion.Version20, "ca.pfx");
            var result = await UnitTestHelpers.Ping(client, c_SELF_SIGNED_URL);
            Assert.True(result == "Pong");
        }

        [Fact]
        public async Task HttpVersion30SelfSignedTest()
        {
            var client = UnitTestHelpers.CreateHttpClient(HttpVersion.Version30, "ca.pfx");
            var result = await UnitTestHelpers.Ping(client, c_SELF_SIGNED_URL);
            Assert.True(result == "Pong");
        }

        [Fact]
        public async Task GrpcHttpVersion30SelfSignedTest()
        {

            var channel = GrpcChannel.ForAddress(c_SELF_SIGNED_URL, 
                new GrpcChannelOptions() { HttpClient = UnitTestHelpers.CreateHttpClient(HttpVersion.Version30, "ca.pfx") });
            var client = new Greeter.GreeterClient(channel);
            var response = await client.SayHelloAsync(new HelloRequest { Name = "World" });
            Assert.True(response.Message == "Hello World");
        }

        [Fact]
        public async Task HttpVersion30OnlySelfSignedTest()
        {
            var client = UnitTestHelpers.CreateHttpClient(HttpVersion.Version30, "ca.pfx");
            var result = await UnitTestHelpers.Ping(client, c_HTTP3_SELF_SIGNED_URL);
            Assert.True(result == "Pong");
        }

        [Fact]
        public async Task HttpVersion30OnlyGrpcTest()
        {
            var channel = GrpcChannel.ForAddress(c_HTTP3_SELF_SIGNED_URL,
                new GrpcChannelOptions() { HttpClient = UnitTestHelpers.CreateHttpClient(HttpVersion.Version30, "ca.pfx") });
            var client = new Greeter.GreeterClient(channel);
            var response = await client.SayHelloAsync(new HelloRequest { Name = "World" });
            Assert.True(response.Message == "Hello World");
        }

        #endregion

        #region Chained Tests

        [Fact]
        public async Task HttpVersion20ChainedTest()
        {
            var client = UnitTestHelpers.CreateHttpClient(HttpVersion.Version20, "client.pfx");
            var result = await UnitTestHelpers.Ping(client, c_CHAINED_URL);
            Assert.True(result == "Pong");
        }

        [Fact]
        public async Task HttpVersion30ChainedTest()
        {
            var client = UnitTestHelpers.CreateHttpClient(HttpVersion.Version30, "client.pfx");
            var result = await UnitTestHelpers.Ping(client, c_CHAINED_URL);
            Assert.True(result == "Pong");
        }

        [Fact]
        public async Task GrpcHttpVersion30ChainedTest()
        {

            var channel = GrpcChannel.ForAddress(c_CHAINED_URL,
                new GrpcChannelOptions() { HttpClient = UnitTestHelpers.CreateHttpClient(HttpVersion.Version30, "client.pfx") });
            var client = new Greeter.GreeterClient(channel);
            var response = await client.SayHelloAsync(new HelloRequest { Name = "World" });
            Assert.True(response.Message == "Hello World");
        }

        [Fact]
        public async Task HttpVersion30OnlyChainedTest()
        {
            var client = UnitTestHelpers.CreateHttpClient(HttpVersion.Version30, "client.pfx");
            var result = await UnitTestHelpers.Ping(client, c_HTTP3_CHAINED_URL);
            Assert.True(result == "Pong");
        }

        [Fact]
        public async Task GrpcHttpVersion30OnlyChainedTest()
        {

            var channel = GrpcChannel.ForAddress(c_HTTP3_CHAINED_URL,
                new GrpcChannelOptions() { HttpClient = UnitTestHelpers.CreateHttpClient(HttpVersion.Version30, "client.pfx") });
            var client = new Greeter.GreeterClient(channel);
            var response = await client.SayHelloAsync(new HelloRequest { Name = "World" });
            Assert.True(response.Message == "Hello World");
        }

        #endregion
    }
}