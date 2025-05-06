namespace Giacom.Cdr.Application
{
    public class CallDetailsOptions
    {
        public string IngestConnectionString { get; set; } = string.Empty;
        public string QueryConnectionString { get; set; } = string.Empty;
        public string Database { get; set; } = string.Empty;
        public string Table { get; set; } = string.Empty;
    }
}
