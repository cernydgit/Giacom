using Microsoft.AspNetCore.TestHost;
using FluentAssertions;
using Alba;

using Giacom.Cdr.Api;
using Giacom.Cdr.Client;
using Giacom.Cdr.UnitTests;
using Giacom.Cdr.Application.CSV;





namespace Giacom.Cdr.IntegrationTests
{
    public class CallDetailTests : IAsyncLifetime
    {
        protected IAlbaHost? Host { get; private set; }
        protected CallDetailsClient? CallDetailsClient { get; private set; }


        [Theory]
        [InlineData("cdr_test_data_2.csv")]
        [InlineData("cdr_test_data_10.csv")]
        [InlineData("cdr_test_data_100MB.csv")]
        [InlineData("cdr_test_data_500MB.csv")]
        [InlineData("cdr_test_data_5GB.csv")]
        public async Task UploadFile_Success(string fileName)
        {
            using FileStream stream = ToStream(fileName);

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

            // no assert - just check if it throws an exception, queued ingestion takes quite a time
        }

        [Theory]
        [InlineData("C1", 100, 100)]
        [InlineData("C1", 1000000, 10000)]
        public async Task QueryByCaller_CorrectResult(string? caller, int? take, int expetedRecordCount)
        {
            // act
            var result = await CallDetailsClient!.CallDetailsAsync(caller, take);

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

        private static FileStream ToStream(string fileName)
        {
            // arrange
            return new FileStream("./TestData/" + fileName, FileMode.Open, FileAccess.Read);
        }
    }
}