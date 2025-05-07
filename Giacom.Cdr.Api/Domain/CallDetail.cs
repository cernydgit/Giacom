namespace Giacom.Cdr.Domain
{
    public record CallDetail
    (
        string Reference,
        long? Caller,
        long? Recipient,
        DateTime? CallEndDateTime,
        int? DurationSec,
        float? Cost,
        string? Currency
    );
}
