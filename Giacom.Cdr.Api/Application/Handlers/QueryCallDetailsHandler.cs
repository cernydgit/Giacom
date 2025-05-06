using Microsoft.Extensions.Options;
using Kusto.Data.Net.Client;
using MediatR;
using Mapster;
using Giacom.Cdr.Domain;
using Giacom.Cdr.Application.DTOs;


namespace Giacom.Cdr.Application.Handlers
{
    public record QueryCallDetailsRequest(string? Caller, int? Take) : IRequest<IEnumerable<CallDetailDto>>{ }

    public class QueryCallDetailsHandler : IRequestHandler<QueryCallDetailsRequest, IEnumerable<CallDetailDto>>
    {
        private readonly IOptions<CallDetailsOptions> options;

        public QueryCallDetailsHandler(IOptions<CallDetailsOptions> options)
        {
            this.options = options;
        }

        public async Task<IEnumerable<CallDetailDto>> Handle(QueryCallDetailsRequest request, CancellationToken cancellationToken)
        {
            var query = $"{options.Value.Table}";

            if (!string.IsNullOrEmpty(request.Caller))
            {
                query += $" | where caller_id == \"{request.Caller}\"";
            }
            if (request.Take.HasValue)
            {
                query += $" | take {request.Take}";
            }
            using var client = KustoClientFactory.CreateCslQueryProvider(options.Value.QueryConnectionString);
            using var reader = await client.ExecuteQueryAsync(options.Value.Database, query, null, cancellationToken);
            var result = reader.Adapt<List<CallDetail>>().Adapt<List<CallDetailDto>>(); 
            return result;
        }
    }
}
