using System.Globalization;
using CsvHelper;
using Mapster;
using FluentAssertions;
using NBench;

using Giacom.Cdr.Domain;
using Giacom.Cdr.Application.CSV;
using Pro.NBench.xUnit.XunitExtensions;

namespace Giacom.Cdr.UnitTests
{
    public class CsvSplitterTests
    {
        [NBenchFact(Skip = "Pre-generated files are not part of repository (too big)")]
        [PerfBenchmark(NumberOfIterations = 5, TestMode = TestMode.Test)]
        [ElapsedTimeAssertion(MaxTimeMilliseconds = 1000 * 10)] // max 10 sec
        [Trait("Category", "Manual")]
        public void SplitCsv5GB_DurationUnder10sec()
        {
            using FileStream stream = new FileStream("C:\\!Code\\!Giacom\\Giacom\\Giacom.Cdr.IntegrationTests\\TestData\\cdr_test_data_5GB.csv", FileMode.Open, FileAccess.Read);
            var tempFiles = CsvSplitter.SplitCsvToTempFiles(stream, "split");
        }

        [Theory]
        [InlineData(1, 1000, 1)]
        [InlineData(10001, 1000, 11)]
        public void SplitCsvFile_HasCorrectStructure(int recordCount, int maxRecordCount, int expectedFileCount)
        {
            TypeAdapterConfig.GlobalSettings.MapModels();

            // arrange: generate a CSV file with data rows (plus header)
            var tempFile = GenerateTemporaryCsvFile(recordCount);
            using FileStream stream = new FileStream(tempFile, FileMode.Open, FileAccess.Read);

            // act: split the CSV file into temporary files
            var tempFiles = CsvSplitter.SplitCsvToTempFiles(stream, "testsplit", maxRecordCount);

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

        public static string GenerateTemporaryCsvFile(long rowCount, string? caller = null)
        {
            var random = new Random();

            string tempFilePath = Path.Combine(Path.GetTempPath(), $"cdr_{Guid.NewGuid()}.csv");

            using (var writer = new StreamWriter(tempFilePath))
            {
                writer.WriteLine("caller_id,recipient,call_date,end_time,duration,cost,reference,currency");

                for (long i = 0; i < rowCount; i++)
                {
                    string callerId = caller ?? Guid.NewGuid().ToString().Substring(0, 15);
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
            return tempFilePath;
        }

    }
}