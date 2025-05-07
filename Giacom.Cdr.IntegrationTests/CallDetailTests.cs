using Microsoft.AspNetCore.TestHost;
using FluentAssertions;
using Alba;

using Giacom.Cdr.Api;
using Giacom.Cdr.Client;
using Giacom.Cdr.UnitTests;


namespace Giacom.Cdr.IntegrationTests
{
    public class CallDetailTests : IAsyncLifetime
    {
        protected IAlbaHost? Host { get; private set; }
        protected CallDetailsClient? CallDetailsClient { get; private set; }


        [Theory(Skip = "Pre-generated files are not part of repository (too big)")]
        [InlineData("cdr_test_data_100MB.csv")]
        [InlineData("cdr_test_data_500MB.csv")]
        [InlineData("cdr_test_data_5GB.csv")]
        public async Task UploadFile_Success(string fileName)
        {
            // arrange
            using FileStream stream = new FileStream("./TestData/" + fileName, FileMode.Open, FileAccess.Read);

            // act
            await CallDetailsClient!.UploadAsync(new FileParameter(stream, fileName));
        }

        [Theory]
        [InlineData(1, "Caller1")]  
        [InlineData(10000, "Caller2")]
        public async Task Upload_Success(long recordCount, string? caller)
        {
            // arrange
            var tempFile = CsvSplitterTests.GenerateTemporaryCsvFile(recordCount, caller);
            using var stream = new FileStream(tempFile, FileMode.Open, FileAccess.Read);

            // act
            await CallDetailsClient!.UploadAsync(new FileParameter(stream, tempFile));

            // no assert (queued ingestion has latency in minutes) - just checked if it throws an exception
        }

        [Theory]
        [InlineData("C1", 100, 100)]
        [InlineData("C1", 1000000, 10000)]
        public async Task QueryByCaller_CorrectResult(string? caller, int? take, int expetedRecordCount)
        {
            // act - querying pre-ingested data
            var result = await CallDetailsClient!.GetByCallerAsync(caller, take);

            // assert
            result.Should().HaveCount(expetedRecordCount);
        }
  
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