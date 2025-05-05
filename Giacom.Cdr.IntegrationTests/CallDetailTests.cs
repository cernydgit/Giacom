using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.TestHost;
using Alba;
using Giacom.Cdr.Api;
using Giacom.Cdr.Client;
using ICSharpCode.SharpZipLib.GZip;


namespace Giacom.Cdr.IntegrationTests
{
    public class CallDetailTests : IAsyncLifetime
    {
        protected IAlbaHost? Host { get; private set; }
        protected CallDetailsClient? CallDetailsClient { get; private set; }
                public async Task InitializeAsync()
        {
            Host = await AlbaHost.For<Program>();
            var httpClient = Host.GetTestClient();
            httpClient.Timeout = TimeSpan.FromMinutes(20);
            CallDetailsClient = new CallDetailsClient(httpClient);
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}