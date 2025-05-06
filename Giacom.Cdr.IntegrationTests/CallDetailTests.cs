using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.TestHost;
using Alba;
using FluentAssertions;
using Giacom.Cdr.Api;
using Giacom.Cdr.Client;
using ICSharpCode.SharpZipLib.GZip;


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
        public async Task UploadCallDetailsFromFile(string fileName)
        {
            // arrange
            using var stream = ResourceToStream(fileName);

            // act
            await CallDetailsClient!.UploadAsync(new FileParameter(stream, fileName));
        }

        [Theory]
        [InlineData(1, "Caller1", false)]
        [InlineData(10000, null, false)]
        [InlineData(10000000, null, false)]
        [InlineData(100, "GZIP2", true)]
        [InlineData(10000000, "GZIP3", true)]
        public async Task UploadCallDetailsGenerated(long recordCount, string? caller, bool compress)
        {
            // arrange
            var tempFile = GenerateTemporaryCsvFile(recordCount, caller, compress);
            using var stream = new FileStream(tempFile, FileMode.Open, FileAccess.Read);

            // act
            await CallDetailsClient!.UploadAsync(new FileParameter(stream, tempFile));

            // no assert - just check if it throws an exception, queued ingestion takes quite a time
        }

        [Theory]
        [InlineData("C1", 100, 100)]
        [InlineData("C1", 1000000, 10000)]
        public async Task QueryCallDetails(string? caller, int? take, int expetedRecordCount)
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
        private static Stream ResourceToStream(string fileName)
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream("Giacom.Cdr.IntegrationTests.TestData." + fileName) ?? throw new FileNotFoundException(fileName);
        }

        private static string GenerateTemporaryCsvFile(long rowCount, string? caller = null, bool compress = false)
        {
            var random = new Random();

            string tempFilePath = Path.Combine(Path.GetTempPath(), $"cdr_{Guid.NewGuid()}.csv");

            using (var writer = new StreamWriter(tempFilePath))
            {
                writer.WriteLine("caller_id,recipient,call_date,end_time,duration,cost,reference,currency");

                for (long i = 0; i < rowCount; i++)
                {
                    string callerId = caller ?? Guid.NewGuid().ToString().Substring(0,15);
                    string recipient = Guid.NewGuid().ToString().Substring(0, 15);
                    string callDate = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
                    string endTime = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
                    int duration = random.Next(1, 1000);
                    decimal cost = random.Next(1, 1000);
                    string reference = Guid.NewGuid().ToString();
                    string currency = "GBP";
                    writer.WriteLine($"{callerId},{recipient},{callDate},{endTime},{duration},{cost},{reference},{currency}");
                }
                writer.Close();
            }

            if (compress)
            {
                string outputFile = tempFilePath + ".gz";

                using (FileStream inputStream = File.OpenRead(tempFilePath))
                using (FileStream outputStream = File.Create(outputFile))
                using (GZipOutputStream gzipStream = new GZipOutputStream(outputStream))
                {
                    inputStream.CopyTo(gzipStream);
                }
                tempFilePath = outputFile;
            }
            return tempFilePath;
        }
    }
}