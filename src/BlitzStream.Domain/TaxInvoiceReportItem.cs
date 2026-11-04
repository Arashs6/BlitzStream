namespace BlitzStream.Domain;

/// <summary>
/// مدل پروجکشن سبک برای گزارش خروجی اکسل از جدول FactInvoice
/// کاملاً Sealed برای حذف اورهد vtable در زمان دسترسی به متدها
/// </summary>
public readonly record struct TaxInvoiceReportItem(
    long Id,
    string? TaxSerialNumber,      // در جدول bigint است و می‌تواند null باشد
    string InvoiceNumber,       // در جدول TaxInvoiceNumber یا InvoiceReferenceId
    DateTime InvoiceDate,       // در جدول InvoiceCreatedAt
    decimal? CashPaymentAmount   // در جدول TotalInvoiceAmount یا CashPaymentAmount
);
//public sealed record TaxInvoiceReportItem
//{
//    public required long Id { get; init; }
//    public string? TaxSerialNumber { get; init; }
//    public required string InvoiceNumber { get; init; }
//    public required DateTime InvoiceDate { get; init; }
//    public string? InvoiceDateShamsi { get; init; }

//    // عناوین و الگوها
//    public string? PatternTitle { get; init; }
//    public string? TypeTitle { get; init; }
//    public string? SubjectTitle { get; init; }
//    public string? StatusTitle { get; init; }

//    // اطلاعات فروشنده و خریدار
//    public string? SellerFiscalId { get; init; }
//    public string? SellerEconomicCode { get; init; }
//    public string? BuyerBusinessName { get; init; }
//    public string? BuyerNationalCode { get; init; }
//    public string? BuyerEconomicCode { get; init; }

//    // مبالغ مالی
//    public decimal? Tax17 { get; init; }
//    public decimal? CashPaymentAmount { get; init; }
//    public decimal? CreditPaymentAmount { get; init; }

//    // اطلاعات سیستمی
//    public int? InvoiceStatus { get; init; }
//    public DateTime CreatedDate { get; init; }
//    public string? CreatedDateShamsi { get; init; }
//}