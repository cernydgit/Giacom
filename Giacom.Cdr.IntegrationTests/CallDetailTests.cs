using System.Diagnostics;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using Alba;

using Giacom.Cdr.Api;
using Giacom.Cdr.Client;
using Giacom.Cdr.UnitTests;
using Giacom.Cdr.Domain.Contracts.Repository;


namespace Giacom.Cdr.IntegrationTests
{
    public class CallDetailTests : IAsyncLifetime
    {
        protected IAlbaHost? Host { get; private set; }
        protected CallDetailsClient? CallDetailsClient { get; private set; }


        [Theory]
        [InlineData("techtest_cdr_5GB.csv")]
        [InlineData("techtest_cdr.csv")]
        public async Task Upload_SampleData_Success(string fileName)
        {
            // arrange
            using FileStream stream = new FileStream("./TestData/" + fileName, FileMode.Open, FileAccess.Read);

            // act
            await CallDetailsClient!.UploadAsync(new FileParameter(stream, fileName));
        }

        [Theory]
        [InlineData(10000, null, false)]
        [InlineData(10000, null, true)]
        public async Task UploadAndQuery_GeneratedData_AllRecordsInserted(int recordCount, long? caller, bool fakeRepostory)
        {
            // arrange
            await CreateHost(fakeRepostory);

            caller ??= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var tempFile = SplitCallDetailsHandlerTests.GenerateTemporaryCsvFile(recordCount, caller);
            using var stream = new FileStream(tempFile, FileMode.Open, FileAccess.Read);

            // act
            await CallDetailsClient!.UploadAsync(new FileParameter(stream, tempFile));

            // assert (queued ingestion can have latency in seconds)
            ICollection<CallDetail> result = null;
            await WaitForAssertAsync(async () =>
                {
                    result = await CallDetailsClient.GetByCallerAsync(caller, recordCount);
                    result.Should().NotBeNull();
                    result.Should().HaveCount(recordCount);
                });

            result.Should().AllSatisfy(callDetail =>
            {
                callDetail.Caller.Should().NotBeNull();
                callDetail.Recipient.Should().NotBeNull();
                callDetail.Reference.Should().NotBeNull();
                callDetail.Cost.Should().NotBeNull();
                callDetail.DurationSec.Should().NotBeNull();
                callDetail.Currency.Should().NotBeNull();
            });
        }

        [Theory]
        [InlineData(null, 100, 100)]
        [InlineData(666L, 100, 100)]
        public async Task QueryByCaller_CorrectResult(long? caller, int? take, int expetedRecordCount)
        {
            // act - querying pre-ingested data
            var result = await CallDetailsClient!.GetByCallerAsync(caller, take);

            // assert
            result.Should().HaveCount(expetedRecordCount);
        }
  
        public async Task InitializeAsync()
        {
            await CreateHost();
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        private async Task CreateHost(bool useFakeCallDetailsRepository = false) 
        {
            // stop original host if exists
            if (Host != null)
            {
                await Host.StopAsync();
                Host.Dispose();
            }

            // create new host
            Host = await AlbaHost.For<Program>(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    if (useFakeCallDetailsRepository)
                    {
                        services.AddSingleton<ICallDetailRepository, FakeCallDetailRepository>();
                    }
                });
            });

            // create API client
            var httpClient = Host.GetTestClient();
            httpClient.Timeout = TimeSpan.FromMinutes(20);
            CallDetailsClient = new CallDetailsClient(httpClient);
        }

        static async Task WaitForAssertAsync(Func<Task> assertion, TimeSpan? timeout = null, TimeSpan? pollInterval = null)
        {
            var actualTimeout = timeout ?? TimeSpan.FromMinutes(10);
            var actualPollInterval = pollInterval ?? TimeSpan.FromSeconds(1);
            var stopwatch = Stopwatch.StartNew();
            Exception lastException = null;

            while (stopwatch.Elapsed < actualTimeout)
            {
                try
                {
                    await assertion();
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }

                await Task.Delay(actualPollInterval);
            }

            // Final attempt: rethrow the last caught exception wrapped in a TimeoutException
            throw new TimeoutException($"Assertion did not pass within {actualTimeout}.", lastException);
        }

    }
}