using BlitzStream.Domain;

namespace BlitzStream.Application;

public interface IInvoiceExportSink
{
    Task ExportToStreamAsync(
        IAsyncEnumerable<TaxInvoiceReportItem> items,
        Stream outputStream,
        CancellationToken cancellationToken = default);
}