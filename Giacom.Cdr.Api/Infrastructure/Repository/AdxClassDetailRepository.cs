using Microsoft.Extensions.Options;
using Kusto.Data.Common;
using Kusto.Ingest;
using Kusto.Data.Net.Client;
using Mapster;
using Giacom.Cdr.Domain.Contracts.Repository;
using Giacom.Cdr.Domain.Entities;

namespace Giacom.Cdr.Infrastructure.Repository
{
    /// <summary>
    /// Repository implementation for managing CallDetails using Azure Data Explorer (ADX).
    /// </summary>
    public class AdxClassDetailRepository : ICallDetailRepository
    {
        private readonly IOptions<AdxCallDetailRepositoryOptions> options;
        private readonly ILogger<AdxClassDetailRepository> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdxClassDetailRepository"/> class.
        /// </summary>
        /// <param name="options">The configuration options for the repository.</param>
        /// <param name="logger">The logger for logging repository operations.</param>
        public AdxClassDetailRepository(IOptions<AdxCallDetailRepositoryOptions> options, ILogger<AdxClassDetailRepository> logger)
        {
            this.options = options;
            this.logger = logger;
        }

        /// <summary>
        /// Ingests a CSV file stream into the Azure Data Explorer (ADX) table.
        /// </summary>
        /// <param name="stream">The input stream containing the CSV data.</param>
        /// <param name="fileId">A unique identifier for the file to avoid duplicate ingestion.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task IngestAsync(Stream stream, string fileId)
        {
            logger.LogInformation("Ingesting file {FileId}", fileId);

            var ingestClient = KustoIngestFactory.CreateQueuedIngestClient(
                options.Value.IngestConnectionString,
                new QueueOptions { MaxRetries = options.Value.IngestMaxRetries });

            using (ingestClient)
            {
                var streamSourceOptions = new StreamSourceOptions
                {
                    Compress = true
                };

                var ingestionProps = new KustoIngestionProperties(options.Value.Database, options.Value.Table)
                {
                    Format = DataSourceFormat.csv,
                    AdditionalTags = [$"Ingested from {Environment.MachineName}, started at {DateTimeOffset.UtcNow}"],
                    IngestByTags = new List<string> { fileId }, // IMPORTANT - Use the file ID as an ingest tag.
                    IngestIfNotExists = new List<string> { fileId } // IMPORTANT - avoid duplicate ingestion.
                };

                // Guaranteed "at least once" delivery
                await ingestClient.IngestFromStreamAsync(stream, ingestionProps, streamSourceOptions);
            }

            logger.LogInformation("Ingested file {FileId}", fileId);
        }

        /// <summary>
        /// Retrieves call details filtered by caller ID and limits the number of results.
        /// </summary>
        /// <param name="caller">The caller ID to filter the results by. If null, no filtering is applied.</param>
        /// <param name="take">The maximum number of records to return. If null, no limit is applied.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A collection of <see cref="CallDetail"/> objects representing the query results.</returns>
        public async Task<IEnumerable<CallDetail>> GetByCallerAsync(long? caller, int? take, CancellationToken cancellationToken)
        {
            // Build KQL query 
            var query = $"{options.Value.Table}";

            if (caller.HasValue)
            {
                query += $" | where caller_id == \"{caller}\"";
            }

            if (take.HasValue)
            {
                query += $" | take {take}";
            }

            // Execute the query using the Kusto client.
            using var client = KustoClientFactory.CreateCslQueryProvider(options.Value.QueryConnectionString);
            using var reader = await client.ExecuteQueryAsync(options.Value.Database, query, null, cancellationToken);

            // Map the query result to domain objects using Mapster.
            var result = reader.Adapt<List<CallDetail>>();
            return result;
        }
    }
}
