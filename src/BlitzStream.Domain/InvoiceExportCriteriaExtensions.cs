namespace BlitzStream.Domain;

public static class InvoiceExportCriteriaExtensions
{
    /// <summary>
    /// اگر بازه تاریخ فاکتور مشخص نشده باشد، مقدار پیش‌فرض می‌گذارد:
    /// ToInvoiceDate = اکنون (UTC) و FromInvoiceDate = یک ماه قبل از ToInvoiceDate.
    /// مقادیری که کاربر فرستاده دست نمی‌خورند.
    /// </summary>
    public static InvoiceExportCriteria WithDefaultDateRange(
        this InvoiceExportCriteria criteria,
        int defaultMonths = 1)
    {
        var to = criteria.ToInvoiceDate ?? DateTime.UtcNow;
        var from = criteria.FromInvoiceDate ?? to.AddMonths(-defaultMonths);

        return criteria with
        {
            FromInvoiceDate = from,
            ToInvoiceDate = to
        };
    }
}
