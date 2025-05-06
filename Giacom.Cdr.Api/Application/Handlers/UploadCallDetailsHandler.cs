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
            var tempFiles = CsvSplitter.SplitCsvToTempFiles(command.Stream, command.FileName);

            await Parallel.ForEachAsync(tempFiles, options.Value.IngestParallelOptions, async (path, ct) =>
            {
                using (var ingestClient = KustoIngestFactory.CreateQueuedIngestClient(options.Value.IngestConnectionString))
                {
                    foreach (var tempFile in tempFiles)
                    {
                        var fileId = Path.GetFileName(tempFile);
                        using var stream = new FileStream(tempFile, FileMode.Open, FileAccess.Read);
                        var ingestionProps = new KustoIngestionProperties(options.Value.Database, options.Value.Table)
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
