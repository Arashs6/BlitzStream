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
        // اولین سطر را قبل از نوشتن هر چیزی می‌خوانیم تا اگر اتصال/کوئری خطا داد،
        // هنوز چیزی به کلاینت ارسال نشده باشد و بتوان پاسخ 500 درست برگرداند.
        await using var enumerator = items.GetAsyncEnumerator(cancellationToken);
        var hasItem = await enumerator.MoveNextAsync();

        // نوشتن BOM برای خوانایی بهتر فارسی در اکسل
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

        // leaveOpen: true می‌گذاریم تا استریم شبکه کلاینت توسط رایتر زودتر بسته نشود
        await using var writer = new StreamWriter(outputStream, encoding, bufferSize: 65536, leaveOpen: true);

        // ۱. نوشتن Header
        await writer.WriteLineAsync("Id,TaxSerialNumber,InvoiceNumber,InvoiceDate,CashPaymentAmount");

        // ۲. استریم مستقیم سطر به سطر
        while (hasItem)
        {
            var item = enumerator.Current;
            var line = string.Create(CultureInfo.InvariantCulture,
                $"{item.Id},{Escape(item.TaxSerialNumber)},{Escape(item.InvoiceNumber)},{item.InvoiceDate:yyyy-MM-dd HH:mm:ss},{item.CashPaymentAmount ?? 0m}");

            await writer.WriteLineAsync(line);
            hasItem = await enumerator.MoveNextAsync();
        }

        await writer.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// مقدار متنی را برای CSV امن می‌کند:
    /// ۱) هر " به "" تبدیل می‌شود و کل مقدار داخل "" قرار می‌گیرد،
    /// ۲) مقادیری که با = + - @ (یا tab/CR) شروع شوند با ' پیشوند می‌خورند تا Excel آن‌ها را فرمول اجرا نکند.
    /// </summary>
    internal static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "\"\"";

        var needsFormulaGuard = value[0] is '=' or '+' or '-' or '@' or '\t' or '\r';

        var sb = new StringBuilder(value.Length + 4);
        sb.Append('"');
        if (needsFormulaGuard) sb.Append('\'');
        sb.Append(value.Replace("\"", "\"\""));
        sb.Append('"');
        return sb.ToString();
    }
}
