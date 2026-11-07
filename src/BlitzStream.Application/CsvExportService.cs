using System.Globalization;
using System.Text;
using BlitzStream.Domain;

namespace BlitzStream.Application;

public class CsvExportService : IInvoiceExportSink
{
    public async Task ExportToStreamAsync(
        IAsyncEnumerable<TaxInvoiceReportItem> items,
        Stream outputStream,
        CancellationToken cancellationToken = default)
    {
        // نوشتن BOM برای خوانایی بهتر فارسی در اکسل
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

        // leaveOpen: true می‌گذاریم تا استریم شبکه کلاینت توسط رایتر زودتر بسته نشود
        await using var writer = new StreamWriter(outputStream, encoding, bufferSize: 65536, leaveOpen: true);

        // ۱. نوشتن Header
        await writer.WriteLineAsync(
            "InvoiceId,InvoiceNumber,IssueDate,CashPaymentAmount,ElectronicPaymentAmount,TotalAmount"
        );

        // ۲. استریم مستقیم سطر به سطر
        await foreach (var item in items.WithCancellation(cancellationToken))
        {
            var line = string.Create(CultureInfo.InvariantCulture,
                $"{item.Id},\"{item.TaxSerialNumber ?? string.Empty}\",\"{item.InvoiceNumber}\",{item.InvoiceDate:yyyy-MM-dd HH:mm:ss},{item.CashPaymentAmount ?? 0m}");

            await writer.WriteLineAsync(line);
        }

        await writer.FlushAsync(cancellationToken);
    }
}