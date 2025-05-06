using Microsoft.Extensions.Options;
using MediatR;
using Kusto.Data.Common;
using Kusto.Ingest;
using Giacom.Cdr.Application.CSV;



namespace Giacom.Cdr.Application.Handlers
{
    public record UploadCallDetailsRequest(Stream Stream, string FileName) : IRequest { }

    public class UploadCallDetailsHandler : IRequestHandler<UploadCallDetailsRequest>
    {
        private readonly IOptions<CallDetailsOptions> options;

        public UploadCallDetailsHandler(IOptions<CallDetailsOptions> options)
        {
            this.options = options;
        }

        public async Task Handle(UploadCallDetailsRequest command, CancellationToken cancellationToken)
        {
            var opts = options.Value; 
            var tempFiles = CsvSplitter.SplitCsvToTempFiles(command.Stream, command.FileName);

            await Parallel.ForEachAsync(tempFiles, opts.IngestParallelOptions, async (path, ct) =>
            {
                var ingestClient = KustoIngestFactory.CreateQueuedIngestClient(
                    opts.IngestConnectionString,
                    new QueueOptions { MaxRetries = opts.IngestMaxRetries });

                using (ingestClient)
                {
                    foreach (var tempFile in tempFiles)
                    {
                        var fileId = Path.GetFileName(tempFile);
                        using var stream = new FileStream(tempFile, FileMode.Open, FileAccess.Read);
                        var ingestionProps = new KustoIngestionProperties(opts.Database, opts.Table)
                        {
                            Format = DataSourceFormat.csv,
                            AdditionalTags = [Path.GetFileName(command.FileName)],
                            IngestByTags = new List<string> { fileId },
                            IngestIfNotExists = new List<string> { fileId }
                        };
                        await ingestClient.IngestFromStreamAsync(stream, ingestionProps);
                    }
                }
            });
        }
    }
}
