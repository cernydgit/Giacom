using Microsoft.Extensions.Options;
using MediatR;
using Kusto.Data.Common;
using Kusto.Ingest;
using Giacom.Cdr.Application.CSV;


namespace Giacom.Cdr.Application.Handlers
{
    /// <summary>
    /// Represents a request to upload call details from a stream with an associated file name.
    /// </summary>
    /// <param name="Stream">The input stream containing the call details in CSV format.</param>
    /// <param name="FileName">The name of the file being uploaded.</param>
    public record UploadCallDetailsRequest(Stream Stream, string FileName) : IRequest { }

    /// <summary>
    /// Handles the <see cref="UploadCallDetailsRequest"/> to process and ingest call details into Kusto.
    /// </summary>
    public class UploadCallDetailsHandler : IRequestHandler<UploadCallDetailsRequest>
    {
        private readonly IOptions<CallDetailsOptions> options;

        /// <summary>
        /// Initializes a new instance of the <see cref="UploadCallDetailsHandler"/> class.
        /// </summary>
        /// <param name="options">The configuration options for handling call details ingestion.</param>
        public UploadCallDetailsHandler(IOptions<CallDetailsOptions> options)
        {
            this.options = options;
        }

        /// <summary>
        /// Handles the upload request by splitting the CSV file and ingesting it into Kusto.
        /// </summary>
        /// <param name="command">The request containing the input stream and file name.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="Exception">Throws if an error occurs during ingestion.</exception>
        public async Task Handle(UploadCallDetailsRequest command, CancellationToken cancellationToken)
        {
            var opts = options.Value;

            // Split the input CSV stream into multiple temporary files for ingestion.
            var tempFiles = CsvSplitter.SplitCsvToTempFiles(command.Stream, command.FileName);

            // Process each temporary file in parallel using the configured parallel options.
            await Parallel.ForEachAsync(tempFiles, opts.IngestParallelOptions, async (path, ct) =>
            {
                var ingestClient = KustoIngestFactory.CreateQueuedIngestClient(
                    opts.IngestConnectionString,
                    new QueueOptions { MaxRetries = opts.IngestMaxRetries });

                using (ingestClient)
                {
                    foreach (var tempFile in tempFiles)
                    {
                        // Generate a unique file ID based on the file name - because of deduplication
                        var fileId = Path.GetFileName(tempFile);

                        // Open the temporary file as a stream for ingestion.
                        using var stream = new FileStream(tempFile, FileMode.Open, FileAccess.Read);

                        // Define the ingestion properties for the Kusto table.
                        var ingestionProps = new KustoIngestionProperties(opts.Database, opts.Table)
                        {
                            Format = DataSourceFormat.csv,
                            AdditionalTags = [Path.GetFileName(command.FileName)], // for tracking only
                            IngestByTags = new List<string> { fileId }, // IMPORTANT - Use the file ID as an ingest tag.
                            IngestIfNotExists = new List<string> { fileId } // IMPORTANT - avoid duplicate ingestion.
                        };

                        // Perform the ingestion of the file stream into Kusto - guarateed "at least once" delivery
                        await ingestClient.IngestFromStreamAsync(stream, ingestionProps);
                    }
                }
            });
        }
    }
}
