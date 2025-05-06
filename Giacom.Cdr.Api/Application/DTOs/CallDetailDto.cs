namespace Giacom.Cdr.Application.DTOs
{
    public record CallDetailDto
    (
        string? Caller,
        string? Recipient,
        DateTime? StartDateTime,
        DateTime? EndDateTime,
        int? Duration
    );
}
