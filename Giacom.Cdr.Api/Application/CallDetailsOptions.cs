namespace Giacom.Cdr.Application
{
    /// <summary>
    /// Represents configuration options for handling call details.
    /// </summary>
    public class CallDetailsOptions
    {

        /// <summary>
        /// Gets or sets the parallel options for controlling the degree of parallelism during data ingestion.
        /// </summary>
        public ParallelOptions IngestParallelOptions { get; set; } = new ParallelOptions { MaxDegreeOfParallelism = 1 };

        /// <summary>
        /// Gets or sets the maximum number of lines allowed per ingestion batch.
        /// Default value corresponds to ~100MB of data.
        /// </summary>
        public int IngestMaxLines { get; set; } = 1_000_000; 

    }
}
