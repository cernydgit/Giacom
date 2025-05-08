namespace Giacom.Cdr.Infrastructure.Repository
{
    /// <summary>
    /// Represents the configuration options for the AdxCallDetailRepository.
    /// </summary>
    public class AdxCallDetailRepositoryOptions
    {
        /// <summary>
        /// Gets or sets the maximum number of retries allowed for ingestion operations in case of failure.
        /// </summary>
        public int IngestMaxRetries { get; set; } = 3;

        /// <summary>
        /// Gets or sets the connection string used for data ingestion into the Kusto database.
        /// </summary>
        public string IngestConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the connection string used for querying data from the Kusto database.
        /// </summary>
        public string QueryConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the database where call details are stored.
        /// </summary>
        public string Database { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the table within the database where call details are stored.
        /// </summary>
        /// <remarks> 
        /// The table should have the following structure:
        /// <code>
        /// .create table CallDetails (caller_id: string, recipient: string, call_end_datetime: datetime, duration: int, cost: real, reference: string, currency: string)  
        /// </code>
        /// </remarks>
        public string Table { get; set; } = string.Empty;
    }
}
