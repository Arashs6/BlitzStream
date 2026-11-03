namespace BlitzStream.Domain;

public sealed record InvoiceExportCriteria
{
    public DateTime? FromInvoiceDate { get; init; }
    public DateTime? ToInvoiceDate { get; init; }
    public string? TaxSerialNumber { get; init; }
    public string? InvoiceNumber { get; init; }
    public string? ReferenceTaxSerialNumber { get; init; }
    public DateTime? FromCreateDate { get; init; }
    public DateTime? ToCreateDate { get; init; }
    public int? BasePaymentTypeId { get; init; }
    public int? BaseInvoicePatternId { get; init; }
    public int? BaseInvoiceSubjectId { get; init; }
    public Guid? BuyerUserId { get; init; }
    public Guid? SellerUserId { get; init; }
    public int? StatusSubId { get; init; }
    public string? CustomsDeclarationCottageNumber { get; init; }
    public string? CooperationCode { get; init; }
}