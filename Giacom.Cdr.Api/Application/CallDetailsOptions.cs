namespace Giacom.Cdr.Application
{
    /// <summary>
    /// Represents configuration options for handling call details, including ingestion and querying settings.
    /// </summary>
    public class CallDetailsOptions
    {
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
        /// Table should have following structure:
        /// <code>
        /// .create table CallDetails (caller_id: string, recipient: string, call_date: datetime, end_time: datetime, duration: long, cost: real, reference: string, currency: string)  
        /// </code>
        /// </remarks>
        public string Table { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the parallel options for controlling the degree of parallelism during data ingestion.
        /// </summary>
        public ParallelOptions IngestParallelOptions { get; set; } = new ParallelOptions { MaxDegreeOfParallelism = 1 };

        /// <summary>
        /// Gets or sets the maximum number of lines allowed per ingestion batch.
        /// Default value corresponds to ~500MB of data.
        /// </summary>
        public long IngestMaxLines { get; set; } = 10000000; 

        /// <summary>
        /// Gets or sets the maximum number of retries allowed for ingestion operations in case of failure.
        /// </summary>
        public int IngestMaxRetries { get; set; } = 3;
    }
}
