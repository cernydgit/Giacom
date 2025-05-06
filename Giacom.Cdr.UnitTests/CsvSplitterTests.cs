using CsvHelper;
using System.Globalization;
using Mapster;
using FluentAssertions;
using Giacom.Cdr.Domain;
using Giacom.Cdr.Application.CSV;

namespace Giacom.Cdr.UnitTests
{
    public class CsvSplitterTests
    {

        [Fact]
        public void SplitCsv5GB_Duration()
        {
            using FileStream stream = new FileStream("C:\\!Code\\!Giacom\\Giacom\\Giacom.Cdr.IntegrationTests\\TestData\\cdr_test_data_5GB.csv", FileMode.Open, FileAccess.Read);
            var tempFiles = CsvSplitter.SplitCsvToTempFiles(stream, "split");
        }

        [Fact]
        public void SplitCsvFile_HasCorrectStructure()
        {
            TypeAdapterConfig.GlobalSettings.MapModels();

            // arrange: generate a CSV file with data rows (plus header)
            int expectedRecordCount = 10000;
            var tempFile = GenerateTemporaryCsvFile(expectedRecordCount);
            using FileStream stream = new FileStream(tempFile, FileMode.Open, FileAccess.Read);

            // act: split the CSV file into temporary files
            var tempFiles = CsvSplitter.SplitCsvToTempFiles(stream, "testsplit", 1000);
            tempFiles.Should().NotBeNull();
            tempFiles.Count.Should().BeGreaterThan(1);

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

            // Verify that the total number of data records equals the expected record count (not counting header rows)
            totalRecords.Should().Be(expectedRecordCount);
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