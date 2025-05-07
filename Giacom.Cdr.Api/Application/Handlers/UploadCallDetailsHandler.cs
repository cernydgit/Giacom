using Microsoft.Extensions.Options;
using MediatR;
using Kusto.Data.Common;
using Kusto.Ingest;



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
        private readonly ISender sender;
        private readonly IOptions<CallDetailsOptions> options;
        private readonly ILogger<UploadCallDetailsHandler> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="UploadCallDetailsHandler"/> class.
        /// </summary>
        /// <param name="options">The configuration options for handling call details ingestion.</param>
        public UploadCallDetailsHandler(ISender sender, IOptions<CallDetailsOptions> options, ILogger<UploadCallDetailsHandler> logger)
        {
            this.sender = sender;
            this.options = options;
            this.logger = logger;
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
            var tempFiles = await sender.Send(new SplitCallDetailsCsvRequest(command.Stream, command.FileName, opts.IngestMaxLines), cancellationToken);

            // Process each temporary file in parallel using the configured parallel options.
            await Parallel.ForEachAsync(tempFiles, opts.IngestParallelOptions, async (path, ct) =>
            {
                logger.LogInformation("Ingesting file {FileName}", path);

                var ingestClient = KustoIngestFactory.CreateQueuedIngestClient(
                    opts.IngestConnectionString,
                    new QueueOptions { MaxRetries = opts.IngestMaxRetries });

                using (ingestClient)
                {
                    // Generate a unique file ID based on the file name - used deduplication
                    var fileId = Path.GetFileName(path);

                    // Open the temporary file as a stream for ingestion.
                    using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);

                    var streamSourceOptions = new StreamSourceOptions
                    {
                         Compress = true
                    };

                    var ingestionProps = new KustoIngestionProperties(opts.Database, opts.Table)
                    {
                        Format = DataSourceFormat.csv,
                        AdditionalTags = [$"Ingested from {Environment.MachineName}, started at {DateTimeOffset.UtcNow}"], 
                        IngestByTags = new List<string> { fileId }, // IMPORTANT - Use the file ID as an ingest tag.
                        IngestIfNotExists = new List<string> { fileId } // IMPORTANT - avoid duplicate ingestion.
                    };

                    // Perform the ingestion of the file stream into ADX
                    // guarateed "at least once" delivery
                    await ingestClient.IngestFromStreamAsync(stream, ingestionProps, streamSourceOptions);
                }

                logger.LogInformation("Ingested file {FileName}", path);
            });
        }
    }
}
