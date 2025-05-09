using System.Text;


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
    public record SplitCallDetailsCsvRequest(Stream Input, string FileNamePrefix, int MaxRows, Encoding? RawEncoding = null)
    {
        public Encoding Encoding => RawEncoding ?? Encoding.UTF8;
    }

    /// <summary>
    /// Handler for <see cref="SplitCallDetailsCsvRequest"/>, executing the split and returning the list of temp file paths.
    /// </summary>
    public sealed class SplitCallDetailsCsvHandler
    {
        private const string expectedHeader = "caller_id,recipient,call_date,end_time,duration,cost,reference,currency";
        private readonly ILogger<SplitCallDetailsCsvHandler> logger;

        public SplitCallDetailsCsvHandler(ILogger<SplitCallDetailsCsvHandler> logger)
        {
            this.logger = logger;
        }

        public Task<IEnumerable<string>> Handle(SplitCallDetailsCsvRequest request, CancellationToken cancellationToken)
        {
            // List to store paths of temporary files created
            var tempFiles = new List<string>();

            // Determine file name prefix, use GUID if not provided
            var prefix = string.IsNullOrWhiteSpace(request.FileNamePrefix)
                ? Guid.NewGuid().ToString()
                : request.FileNamePrefix!;

            // Create a StreamReader to read the input CSV
            using var reader = new StreamReader(
                request.Input,
                request.Encoding,
                detectEncodingFromByteOrderMarks: true,
                leaveOpen: true,
                bufferSize: 1 << 20);

            // Read the header line from the CSV
            var header = reader.ReadLine();

            if (header == null)
                return Task.FromResult<IEnumerable<string>>(tempFiles);

            // Validate the CSV header format
            if (string.Join(",", header.Split(',').Select(s => s.Trim())) != expectedHeader)
            {
                throw new InvalidDataException($"Invalid format of input file. File prefix: {request.FileNamePrefix}, CSV header: {header}. Expected: {expectedHeader}");
            }

            int fileIndex = 0;
            CsvChunk? chunk = null;

            try
            {
                // Create the first chunk and add its file path to the list
                chunk = CreateChunk(prefix, fileIndex++, request.Encoding);
                tempFiles.Add(chunk.FilePath);

                string? line;

                // Process each line of the CSV
                while (!cancellationToken.IsCancellationRequested && (line = reader.ReadLine()) != null)
                {
                    // If the current chunk reaches max rows, finalize it and create a new chunk
                    if (chunk.CurrentRow >= request.MaxRows)
                    {
                        chunk.Dispose();
                        chunk = CreateChunk(prefix, fileIndex++, request.Encoding);
                        tempFiles.Add(chunk.FilePath);
                    }

                    // Write the current line to the active chunk
                    chunk.WriteLine(line);
                }
            }
            finally
            {
                // Ensure the last chunk is properly disposed
                chunk?.Dispose();
            }

            // Return the list of temporary file paths
            return Task.FromResult<IEnumerable<string>>(tempFiles);
        }

        private CsvChunk CreateChunk(string prefix, int index, Encoding encoding)
        {
            // Generate a temporary file path for the chunk
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

                // Initialize file stream and writer
                stream = new FileStream(
                    path: filePath,
                    mode: FileMode.Create,
                    access: FileAccess.Write,
                    share: FileShare.None,
                    bufferSize: 81920,
                    options: FileOptions.SequentialScan);
                writer = new StreamWriter(stream, encoding) { AutoFlush = false };

                // Write the header to the chunk file
                writer.WriteLine(header);
                writer.Flush();
                CurrentRow = 0;
            }

            public void WriteLine(string line)
            {
                // transformed line and write it to the chunk file
                // transform the line to merge all_date and call_end_time
                writer.WriteLine(TransformLine(line));
                CurrentRow++;
            }

            string TransformLine(string line)
            {
                try
                {
                    // Merge call_date and call_end_time into a single field
                    var dateTimeIndex = FindChar(line, ',', 3);
                    return string.Concat(line.Substring(0, dateTimeIndex).TrimEnd(), " ", line.Substring(dateTimeIndex + 1).TrimStart());
                }
                catch (Exception ex)
                {
                    // Handle transformation errors
                    throw new InvalidDataException($"Error transforming line: {line} of file:{FilePath}, row:{CurrentRow}", ex);
                }
            }

            static int FindChar(string input, char c, int order)
            {
                // Find the nth occurrence of a character in a string
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
                // Ensure all data is flushed and resources are released
                writer.Flush();
                writer.Dispose();
                stream.Dispose();
            }
        }
    }
}