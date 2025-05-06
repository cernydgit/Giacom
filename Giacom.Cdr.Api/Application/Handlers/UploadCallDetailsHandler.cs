using Microsoft.Extensions.Options;
using MediatR;
using Kusto.Data.Common;
using Kusto.Ingest;



namespace Giacom.Cdr.Application.Handlers
{
    public record UploadCallDetailsRequest(Stream Stream, bool Compressed) : IRequest { }

    public class UploadCallDetailsHandler : IRequestHandler<UploadCallDetailsRequest>
    {
        private readonly IOptions<CallDetailsOptions> options;

        public UploadCallDetailsHandler(IOptions<CallDetailsOptions> options)
        {
            this.options = options;
        }

        public async Task Handle(UploadCallDetailsRequest command, CancellationToken cancellationToken)
        {
            using (var ingestClient = KustoIngestFactory.CreateQueuedIngestClient(options.Value.IngestConnectionString))
            {
                var streamSourceOptions = new StreamSourceOptions
                {
                    CompressionType = command.Compressed ? DataSourceCompressionType.GZip : DataSourceCompressionType.None,
                };

                var ingestionProps = new KustoIngestionProperties(options.Value.Database, options.Value.Table)
                {
                    Format = DataSourceFormat.csv
                };

                await ingestClient.IngestFromStreamAsync(command.Stream, ingestionProps, streamSourceOptions);
            }
        }
    }
}
