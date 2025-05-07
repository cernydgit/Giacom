using Microsoft.Extensions.Options;
using Kusto.Data.Net.Client;
using MediatR;
using Mapster;
using Giacom.Cdr.Domain;
using Giacom.Cdr.Application.DTOs;


namespace Giacom.Cdr.Application.Handlers
{
    /// <summary>
    /// Represents a request to query call details with optional filters for Caller and Take (limit).
    /// </summary>
    /// <param name="Caller">The caller ID to filter the results by. If null, no filtering is applied.</param>
    /// <param name="Take">The maximum number of records to return. If null, no limit is applied.</param>
    public record QueryCallDetailsRequest(string? Caller, int? Take) : IRequest<IEnumerable<CallDetailDto>> { }

    /// <summary>
    /// Handles the QueryCallDetailsRequest and retrieves call details from the database.
    /// </summary>
    public class QueryCallDetailsHandler : IRequestHandler<QueryCallDetailsRequest, IEnumerable<CallDetailDto>>
    {
        private readonly IOptions<CallDetailsOptions> options;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryCallDetailsHandler"/> class.
        /// </summary>
        /// <param name="options">The configuration options for querying call details.</param>
        public QueryCallDetailsHandler(IOptions<CallDetailsOptions> options)
        {
            this.options = options;
        }

        /// <summary>
        /// Handles the request to query call details.
        /// </summary>
        /// <param name="request">The request containing the query parameters.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A collection of <see cref="CallDetailDto"/> objects representing the query results.</returns>
        /// <exception cref="Exception">Throws if an error occurs during query execution or mapping.</exception>
        public async Task<IEnumerable<CallDetailDto>> Handle(QueryCallDetailsRequest request, CancellationToken cancellationToken)
        {
            // Build the KQL query 
            var query = $"{options.Value.Table}";

            if (!string.IsNullOrEmpty(request.Caller))
            {
                query += $" | where caller_id == \"{request.Caller}\"";
            }

            if (request.Take.HasValue)
            {
                query += $" | take {request.Take}";
            }

            // Execute the query using the Kusto client.
            using var client = KustoClientFactory.CreateCslQueryProvider(options.Value.QueryConnectionString);
            using var reader = await client.ExecuteQueryAsync(options.Value.Database, query, null, cancellationToken);

            // Map the query result to domain objects and then to DTOs using Mapster.
            var result = reader.Adapt<List<CallDetail>>().Adapt<List<CallDetailDto>>();
            return result;
        }
    }
}
