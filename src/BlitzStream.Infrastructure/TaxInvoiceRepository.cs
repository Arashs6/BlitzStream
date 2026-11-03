using BlitzStream.Domain;
using BlitzStream.Domain.Contracts;

namespace BlitzStream.Infrastructure;

public class TaxInvoiceRepository : ITaxInvoiceRepository
{
    public IAsyncEnumerable<TaxInvoiceReportItem> StreamInvoicesAsync(InvoiceExportCriteria criteria, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}