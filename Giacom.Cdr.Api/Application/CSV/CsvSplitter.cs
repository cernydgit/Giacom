using System.Text;

namespace Giacom.Cdr.Application.CSV
{
    public static class CsvSplitter
    {
        /// <summary>
        /// Splits an input CSV stream into multiple temporary files, each not exceeding maxChunkSize.
        /// Each chunk will include the header line.
        /// Returns the list of temp file paths.
        /// </summary>
        /// <param name="input">Input CSV stream (must support reading).</param>
        /// <param name="fileNamePrefix">Optional prefix for temp file names; if null, a GUID is used.</param>
        /// <param name="maxChunkSize">Maximum size of each chunk in bytes (default 500 MB).</param>
        /// <param name="encoding">Text encoding (default UTF8).</param>
        public static List<string> SplitCsvToTempFiles(
            Stream input,
            string? fileNamePrefix = null,
            long maxChunkSize = 500L * 1024 * 1024,
            Encoding? encoding = null)
        {
            encoding ??= Encoding.UTF8;
            var tempFiles = new List<string>();

            // Determine prefix: use provided or generate GUID
            var prefix = string.IsNullOrWhiteSpace(fileNamePrefix)
                ? Guid.NewGuid().ToString()
                : fileNamePrefix;

            using var reader = new StreamReader(input, encoding, detectEncodingFromByteOrderMarks: true, leaveOpen: true);

            // Read header line
            var header = reader.ReadLine();
            if (header == null)
                return tempFiles;

            FileStream? fs = null;
            StreamWriter? writer = null;
            long currentSize = 0;

            // Local function to start a new temp file
            void StartNewFile()
            {
                // Close and dispose previous writer and stream
                if (writer != null)
                {
                    writer.Flush();
                    writer.Dispose();
                    fs?.Dispose();
                }

                // Create new temp file with prefix and index
                var index = tempFiles.Count;
                var tempPath = Path.Combine(
                    Path.GetTempPath(),
                    $"{prefix}_{index}.csv");

                fs = new FileStream(
                    path: tempPath,
                    mode: FileMode.Create,
                    access: FileAccess.Write,
                    share: FileShare.None,
                    bufferSize: 81920,
                    options: FileOptions.None);
                writer = new StreamWriter(fs, encoding) { AutoFlush = false };

                // Write header
                writer.WriteLine(header);
                currentSize = encoding.GetByteCount(header + Environment.NewLine);

                tempFiles.Add(tempPath);
            }

            // Initialize first chunk
            StartNewFile();

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var lineBytes = encoding.GetByteCount(line + Environment.NewLine);

                if (currentSize + lineBytes > maxChunkSize)
                {
                    StartNewFile();
                }

                writer!.WriteLine(line);
                currentSize += lineBytes;
            }

            // Finalize last file
            if (writer != null)
            {
                writer.Flush();
                writer.Dispose();
                fs?.Dispose();
            }

            return tempFiles;
        }
    }
}
