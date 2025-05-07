using System.Text;
using MediatR;

namespace Giacom.Cdr.Application.Handlers
{
    /// <summary>
    /// Request to split a large CallDetails CSV into smaller temporary files, each containing a header and up to <see cref="MaxRows"/> data rows.
    /// </summary>
    /// <remarks>
    /// Each line of the CSV will be transformed - it merges call_date + end_time into end_call_datetime
    /// </remarks>
    /// <param name="Input">The input Call Details CSV <see cref="Stream"/> to read from.</param>
    /// <param name="FileNamePrefix">Optional prefix for temporary file names.</param>
    /// <param name="MaxRows">Maximum number of data rows per chunk.</param>
    /// <param name="RawEncoding">The text encoding of the CSV, defaults to UTF8 if not provided.</param>
    public record SplitCallDetailsCsvRequest(Stream Input, string FileNamePrefix, int MaxRows, Encoding? RawEncoding = null) : IRequest<IEnumerable<string>>
    {
        public Encoding Encoding => RawEncoding ?? Encoding.UTF8;
    }

    /// <summary>
    /// Handler for <see cref="SplitCallDetailsCsvRequest"/>, executing the split and returning the list of temp file paths.
    /// </summary>
    public sealed class SplitCallDetailsCsvHandler : IRequestHandler<SplitCallDetailsCsvRequest, IEnumerable<string>>
    {
        private readonly ILogger<SplitCallDetailsCsvHandler> logger;

        public SplitCallDetailsCsvHandler(ILogger<SplitCallDetailsCsvHandler> logger)
        {
            this.logger = logger;
        }


        /// <inheritdoc/>
        public Task<IEnumerable<string>> Handle(SplitCallDetailsCsvRequest request, CancellationToken cancellationToken)
        {
            var tempFiles = new List<string>();
            var prefix = string.IsNullOrWhiteSpace(request.FileNamePrefix)
                ? Guid.NewGuid().ToString()
                : request.FileNamePrefix!;

            using var reader = new StreamReader(
                request.Input,
                request.Encoding,
                detectEncodingFromByteOrderMarks: true,
                leaveOpen: true,
                bufferSize: 1 << 20);

            if (reader.ReadLine() == null)
                return Task.FromResult<IEnumerable<string>>(tempFiles);

            int fileIndex = 0;
            CsvChunk? chunk = null;
            try
            {
                chunk = CreateChunk(prefix, fileIndex++, request.Encoding);
                tempFiles.Add(chunk.FilePath);

                string? line;
                while (!cancellationToken.IsCancellationRequested && (line = reader.ReadLine()) != null)
                {
                    if (chunk.CurrentRow >= request.MaxRows)
                    {
                        chunk.Dispose();
                        chunk = CreateChunk(prefix, fileIndex++, request.Encoding);
                        tempFiles.Add(chunk.FilePath);
                    }

                    chunk.WriteLine(line);
                }
            }
            finally
            {
                chunk?.Dispose();
            }

            return Task.FromResult<IEnumerable<string>>(tempFiles);
        }

        private CsvChunk CreateChunk(string prefix, int index, Encoding encoding)
        {
            var filePath = Path.Combine(Path.GetTempPath(), $"{prefix}_{index}.csv");
            logger.LogInformation("Creating csv chunk file: {FilePath}", filePath);
            return new CsvChunk(filePath, "caller_id,recipient,call_end_datetime,duration,cost,reference,currency", encoding);
        }

        private sealed class CsvChunk : IDisposable
        {
            private readonly FileStream stream;
            private readonly StreamWriter writer;

            public string FilePath { get; }

            public int CurrentRow { get; private set; }

            public CsvChunk(string filePath, string header, Encoding encoding)
            {
                FilePath = filePath;
                stream = new FileStream(
                    path: filePath,
                    mode: FileMode.Create,
                    access: FileAccess.Write,
                    share: FileShare.None,
                    bufferSize: 81920,
                    options: FileOptions.SequentialScan);
                writer = new StreamWriter(stream, encoding) { AutoFlush = false };

                writer.WriteLine(header);
                writer.Flush();
                CurrentRow = 0;
            }

            public void WriteLine(string line)
            {
                writer.WriteLine(TransformLine(line));
                CurrentRow++;
            }

            string TransformLine(string line)
            {
                try
                {
                    var dateTimeIndex = FindChar(line, ',', 3);
                    return string.Concat(line.Substring(0, dateTimeIndex).TrimEnd(), " ", line.Substring(dateTimeIndex + 1).TrimStart());
                }
                catch (Exception ex)
                {
                    throw new InvalidDataException($"Error transforming line: {line} of file:{FilePath}, row:{CurrentRow}", ex);
                }
            }

            static int FindChar(string input, char c, int order)
            {
                int count = 0;
                for (int i = 0; i < input.Length; i++)
                {
                    if (input[i] == c)
                    {
                        count++;
                        if (count == order) return i;
                    }
                }
                return -1;
            }

            public void Dispose()
            {
                writer.Flush();
                writer.Dispose();
                stream.Dispose();
            }
        }
    }
}