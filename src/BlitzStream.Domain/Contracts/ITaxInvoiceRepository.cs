using System.Runtime.CompilerServices;

namespace BlitzStream.Domain.Contracts;

public interface ITaxInvoiceRepository
{
    IAsyncEnumerable<TaxInvoiceReportItem> StreamInvoicesAsync(
        InvoiceExportCriteria criteria,
        CancellationToken cancellationToken = default);
}