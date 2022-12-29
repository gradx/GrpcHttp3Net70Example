using System.Net;

namespace Http3GrpcUnitTest
{
    public class UnitTest1
    {
        public const string c_SELF_SIGNED_URL = "https://mydomain.com:5001";
        public const string c_CHAINED_URL = "https://chained.mydomain.com:5002";

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
    }
}