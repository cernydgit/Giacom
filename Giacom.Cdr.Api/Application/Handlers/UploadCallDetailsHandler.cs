using Microsoft.Extensions.Options;
using MediatR;
using Giacom.Cdr.Domain.Contracts.Repository;
using Giacom.Cdr.Application.Common;



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
        private readonly IFactory<ICallDetailRepository> repositoryFactory;
        private readonly ISender sender;
        private readonly IOptions<CallDetailsOptions> options;

        /// <summary>
        /// Initializes a new instance of the <see cref="UploadCallDetailsHandler"/> class.
        /// </summary>
        /// <param name="options">The configuration options for handling call details ingestion.</param>
        public UploadCallDetailsHandler(
            IFactory<ICallDetailRepository> repositoryFactory, 
            ISender sender, 
            IOptions<CallDetailsOptions> options)

        {
            this.repositoryFactory = repositoryFactory;
            this.sender = sender;
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
            // Split the input CSV stream into multiple temporary files for ingestion.
            var tempFiles = await sender.Send(
                new SplitCallDetailsCsvRequest(command.Stream, Path.GetFileNameWithoutExtension(command.FileName), options.Value.IngestMaxLines),
                cancellationToken);

            await Parallel.ForEachAsync(tempFiles, options.Value.IngestParallelOptions, async (path, ct) =>
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);

                // Generate a unique file ID based on the file name - used for deduplication
                var fileId = Path.GetFileName(path);

                // Ingest the file stream using the repository
                // factory pattern used becouse of thread-safety of repository implementation is unknown
                await repositoryFactory.Create().IngestAsync(stream, fileId);
            });
        }
    }
}
