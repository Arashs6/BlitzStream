namespace BlitzStream.Domain;

/// <summary>
/// مدل پروجکشن سبک برای گزارش خروجی از جدول FactInvoice
/// (struct است، پس روی heap تخصیص نمی‌یابد)
/// </summary>
public readonly record struct TaxInvoiceReportItem(
    long Id,
    string? TaxSerialNumber,      // در جدول می‌تواند bigint و null باشد؛ به string تبدیل می‌شود
    string InvoiceNumber,
    DateTime InvoiceDate,
    decimal? CashPaymentAmount
);
