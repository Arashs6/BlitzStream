namespace BlitzStream.Domain;

/// <summary>
/// فقط فیلترهایی که واقعاً در کوئری اعمال می‌شوند اینجا هستند.
/// فیلتر جدید باید همزمان در TaxInvoiceRepository هم پیاده شود.
/// </summary>
public sealed record InvoiceExportCriteria
{
    public DateTime? FromInvoiceDate { get; init; }
    public DateTime? ToInvoiceDate { get; init; }
    public string? TaxSerialNumber { get; init; }
    public string? InvoiceNumber { get; init; }
}
