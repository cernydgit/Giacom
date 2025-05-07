using System.Globalization;
using Microsoft.Extensions.Logging;
using CsvHelper;
using Mapster;
using FluentAssertions;
using NBench;
using Pro.NBench.xUnit.XunitExtensions;
using Moq;

using Giacom.Cdr.Application.Handlers;
using Giacom.Cdr.Domain;

namespace Giacom.Cdr.UnitTests
{
    public class SplitCallDetailsHandlerTests
    {
        private Mock<ILogger<SplitCallDetailsCsvHandler>>  loggerMock = new Mock<ILogger<SplitCallDetailsCsvHandler>>();

        //[NBenchFact(Skip = "Pre-generated files are not part of repository (too big)")]
        [NBenchFact]
        [PerfBenchmark(NumberOfIterations = 1, TestMode = TestMode.Test)]
        [ElapsedTimeAssertion(MaxTimeMilliseconds = 1000 * 30)] // max 30 sec
        [Trait("Category", "Manual")]
        [Trait("Category", "LongRunning")]
        public void SplitCsv5GB_DurationCheck()
        {
            // arrange: use a pre-generated 5GB CSV file
            using FileStream stream = new FileStream("./TestData/techtest_cdr_5GB.csv", FileMode.Open, FileAccess.Read);
            var request = new SplitCallDetailsCsvRequest(stream, "split", 1000000);

            // act: split the CSV file into temporary files
            new SplitCallDetailsCsvHandler(loggerMock.Object).Handle(request, default).Wait();
        }

        [Theory]
        [InlineData(1, 1000, 1)]
        [InlineData(10001, 1000, 11)]
        [InlineData(10000, 1000, 10)]
        public async Task SplitCsvFile_HasCorrectStructure(int recordCount, int maxRecordCount, int expectedFileCount)
        {
            TypeAdapterConfig.GlobalSettings.MapModels();

            // arrange: generate a CSV file with data rows (plus header)
            var tempFile = GenerateTemporaryCsvFile(recordCount);
            using FileStream stream = new FileStream(tempFile, FileMode.Open, FileAccess.Read);

            // act: split the CSV file into temporary files
            var request = new SplitCallDetailsCsvRequest(stream, "split", maxRecordCount);
            var tempFiles = await new SplitCallDetailsCsvHandler(loggerMock.Object).Handle(request, default);

            // assert: Check that the number of temporary files created matches the expected count
            tempFiles.Should().NotBeNull();
            tempFiles.Should().HaveCount(expectedFileCount);

            // assert: Use CsvHelper to read each temp file and sum the total number of records
            int totalRecords = 0;
            foreach (var filePath in tempFiles)
            {
                using var reader = new StreamReader(filePath);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                // This will map records to the CallDetail class. An exception here would indicate a structure issue.
                var records = new CsvDataReader(csv).ToCallDetails();
                records.Should().NotBeNull();
                totalRecords += records.Count;
            }

            //  assert: Check that the total number of records across all temp files matches the original record count
            totalRecords.Should().Be(recordCount);
        }

        public static string GenerateTemporaryCsvFile(long rowCount, long? caller = null)
        {
            var random = new Random();

            string tempFilePath = Path.Combine(Path.GetTempPath(), $"cdr_{Guid.NewGuid()}.csv");

            using (var writer = new StreamWriter(tempFilePath))
            {
                writer.WriteLine("caller_id,recipient,call_date,end_time,duration,cost,reference,currency");

                for (long i = 0; i < rowCount; i++)
                {
                    long callerId = caller ?? random.NextInt64();
                    long recipient = random.NextInt64();
                    var callDate = DateOnly.FromDateTime(DateTime.UtcNow);
                    var endTime = TimeOnly.FromDateTime(DateTime.UtcNow);
                    int duration = random.Next(1, 1000);
                    int cost = random.Next(1, 1000);
                    string reference = Guid.NewGuid().ToString();
                    string currency = "GBP";
                    writer.WriteLine($"{callerId},{recipient},{callDate},{endTime:HH:mm:ss},{duration},{cost},{reference},{currency}");
                }
                writer.Close();
            }
            return tempFilePath;
        }
    }
}