using System.Text;

namespace Giacom.Cdr.Application.CSV
{
    public static class CsvSplitter
    {
        /// <summary>
        /// Splits a CSV file stream into multiple temporary files, each containing at most maxRows data lines (excluding header).
        /// Each chunk will repeat the header line.
        /// Returns the list of temp file paths.
        /// </summary>
        /// <param name="input">Input CSV stream (must support reading).</param>
        /// <param name="fileNamePrefix">Optional prefix for temp file names; if null, a GUID is used.</param>
        /// <param name="maxRows">Maximum number of data rows per chunk (default 10000000).</param>
        /// <param name="encoding">Text encoding (default UTF8).</param>
        public static List<string> SplitCsvToTempFiles(
            Stream input,
            string? fileNamePrefix = null,
            int maxRows = 10000000,
            Encoding? encoding = null)
        {
            // Default to UTF8 encoding if none is provided
            encoding ??= Encoding.UTF8;
            // Initialize a list to store the paths of the temporary files created
            var tempFiles = new List<string>();
            // Determine the prefix for file names; if not provided or empty, generate a new GUID as the prefix
            var prefix = string.IsNullOrWhiteSpace(fileNamePrefix)
                ? Guid.NewGuid().ToString()
                : fileNamePrefix;

            // Create a StreamReader to read the input CSV with the specified encoding and buffer size.
            using var reader = new StreamReader(
                input,
                encoding,
                detectEncodingFromByteOrderMarks: true,
                leaveOpen: true,
                bufferSize: 1 << 20);

            // Read the first line as header. If no header, return the empty list.
            var header = reader.ReadLine();
            if (header == null)
                return tempFiles;

            int fileIndex = 0;
            CsvChunk? chunk = null;
            try
            {
                // Create the first chunk with the header and increment the file index
                chunk = CreateChunk(prefix, fileIndex++, header, encoding);
                // Add the file path of this chunk to our list
                tempFiles.Add(chunk.FilePath);

                string? line;
                // Loop through each remaining line in the CSV file
                while ((line = reader.ReadLine()) != null)
                {
                    // If the current chunk has reached the maximum number of data rows,
                    // then dispose the current chunk and create a new one.
                    if (chunk.CurrentRow >= maxRows)
                    {
                        // Flush and close the current chunk
                        chunk.Dispose();
                        // Create a new chunk with a new file index and add its file path to our list
                        chunk = CreateChunk(prefix, fileIndex++, header, encoding);
                        tempFiles.Add(chunk.FilePath);
                    }

                    // Write the current line into the active chunk and update the row counter
                    chunk.WriteLine(line);
                }
            }
            finally
            {
                // Ensure the last chunk is properly disposed to flush any remaining data.
                chunk?.Dispose();
            }

            // Return the list of temporary file paths generated
            return tempFiles;
        }

        private static CsvChunk CreateChunk(string prefix, int index, string header, Encoding encoding)
        {
            // Combine the system's temporary folder path with a new file name using the prefix and index
            var filePath = Path.Combine(Path.GetTempPath(), $"{prefix}_{index}.csv");
            // Create and return a new CsvChunk instance which initializes the file with the header.
            return new CsvChunk(filePath, header, encoding);
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
                // Create a FileStream for writing to the temporary CSV file with sequential scan optimization.
                stream = new FileStream(
                    path: filePath,
                    mode: FileMode.Create,
                    access: FileAccess.Write,
                    share: FileShare.None,
                    bufferSize: 81920,
                    options: FileOptions.SequentialScan);
                // Create a StreamWriter using the provided encoding; disable auto-flush for performance reasons.
                writer = new StreamWriter(stream, encoding) { AutoFlush = false };

                // Write the header line to the file
                writer.WriteLine(header);
                // Initialize the data row counter to zero since only the header is written so far.
                CurrentRow = 0;
            }

            public void WriteLine(string line)
            {
                writer.WriteLine(line);
                CurrentRow++;
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
