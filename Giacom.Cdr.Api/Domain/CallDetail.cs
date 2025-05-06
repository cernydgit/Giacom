namespace Giacom.Cdr.Domain
{
    public record CallDetail
    (
        string Reference,
        string? Caller,
        string? Recipient,
        DateTime? StartDateTime,
        DateTime? EndDateTime,
        int? Duration,
        decimal? Cost,
        string? Currency
    );


}
