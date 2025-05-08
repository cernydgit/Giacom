using Giacom.Cdr.Domain.Entities;

namespace Giacom.Cdr.Domain.Contracts.Repository
{
    /// <summary>
    /// Interface for managing <see cref="CallDetail"/> records in the repository.
    /// </summary>
    public interface ICallDetailRepository
    {
        /// <summary>
        /// Ingests call detail records from a stream into the repository.
        /// </summary>
        /// <param name="stream">The stream containing call detail records.</param>
        /// <param name="fileId">The unique identifier for the file being ingested.</param>
        Task IngestAsync(Stream stream, string fileId);

        /// <summary>
        /// Retrieves call detail records by caller ID.
        /// </summary>
        /// <param name="caller">The caller ID to filter the records. Null to retrieve all records.</param>
        /// <param name="take">The maximum number of records to retrieve. Null to retrieve all available records.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>Collection of <see cref="CallDetail"/>  records.</returns>
        Task<IEnumerable<CallDetail>> GetByCallerAsync(long? caller, int? take, CancellationToken cancellationToken);
    }
}