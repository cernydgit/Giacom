namespace Giacom.Cdr.Domain.Entities
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
