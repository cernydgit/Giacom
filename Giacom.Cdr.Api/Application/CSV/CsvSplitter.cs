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
            encoding ??= Encoding.UTF8;
            var tempFiles = new List<string>();
            var prefix = string.IsNullOrWhiteSpace(fileNamePrefix)
                ? Guid.NewGuid().ToString()
                : fileNamePrefix;

            using var reader = new StreamReader(
                input,
                encoding,
                detectEncodingFromByteOrderMarks: true,
                leaveOpen: true,
                bufferSize: 1 << 20);

            var header = reader.ReadLine();
            if (header == null)
                return tempFiles;

            int fileIndex = 0;
            CsvChunk? chunk = null;
            try
            {
                chunk = CreateChunk(prefix, fileIndex++, header, encoding);
                tempFiles.Add(chunk.FilePath);

                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (chunk.CurrentRow >= maxRows)
                    {
                        chunk.Dispose();
                        chunk = CreateChunk(prefix, fileIndex++, header, encoding);
                        tempFiles.Add(chunk.FilePath);
                    }

                    chunk.WriteLine(line);
                }
            }
            finally
            {
                chunk?.Dispose();
            }

            return tempFiles;
        }

        private static CsvChunk CreateChunk(string prefix, int index, string header, Encoding encoding)
        {
            var filePath = Path.Combine(Path.GetTempPath(), $"{prefix}_{index}.csv");
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
                stream = new FileStream(
                    path: filePath,
                    mode: FileMode.Create,
                    access: FileAccess.Write,
                    share: FileShare.None,
                    bufferSize: 81920,
                    options: FileOptions.SequentialScan);
                writer = new StreamWriter(stream, encoding) { AutoFlush = false };

                // Write header
                writer.WriteLine(header);
                stream.Flush();
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
