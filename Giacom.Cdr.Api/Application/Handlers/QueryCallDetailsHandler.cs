using Microsoft.Extensions.Options;

using Giacom.Cdr.Domain.Entities;
using Giacom.Cdr.Domain.Contracts.Repository;

namespace Giacom.Cdr.Application.Handlers
{
    /// <summary>
    /// Represents a request to query call details with optional filters for Caller and Take (limit).
    /// </summary>
    /// <param name="Caller">The caller ID to filter the results by. If null, no filtering is applied.</param>
    /// <param name="Take">The maximum number of records to return. If null, no limit is applied.</param>
    public record QueryCallDetailsRequest(long? Caller, int? Take)  { }

    /// <summary>
    /// Handles the QueryCallDetailsRequest and retrieves call details from the database.
    /// </summary>
    public class QueryCallDetailsHandler 
    {
        private readonly ICallDetailRepository repository;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryCallDetailsHandler"/> class.
        /// </summary>
        /// <param name="options">The configuration options for querying call details.</param>
        public QueryCallDetailsHandler(ICallDetailRepository repository,  IOptions<CallDetailsOptions> options)
        {
            this.repository = repository;
        }

        /// <summary>
        /// Handles the request to query call details.
        /// </summary>
        /// <param name="request">The request containing the query parameters.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A collection of <see cref="CallDetail"/> objects representing the query results.</returns>
        /// <exception cref="Exception">Throws if an error occurs during query execution or mapping.</exception>
        public async Task<IEnumerable<CallDetail>> Handle(QueryCallDetailsRequest request, CancellationToken cancellationToken)
        {
            return await repository.GetByCallerAsync(request.Caller,request.Take, cancellationToken);
        }

    }
}
