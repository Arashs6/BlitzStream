using BlitzStream.Domain;
using BlitzStream.Domain.Contracts;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace BlitzStream.Infrastructure;

public class TaxInvoiceRepository : ITaxInvoiceRepository
{
    private readonly string _connectionString;

    public TaxInvoiceRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Warehouse")
                            ?? throw new InvalidOperationException("Connection string 'Warehouse' not found.");
    }

    public async IAsyncEnumerable<TaxInvoiceReportItem> StreamInvoicesAsync(
        InvoiceExportCriteria criteria,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand { Connection = connection, CommandType = CommandType.Text };

        // جلوگیری از تایم‌اوت در کوئری‌های سنگین مالیاتی
        command.CommandTimeout = 120;
        command.CommandText = BuildQuery(criteria, command.Parameters);

        // SequentialAccess برای استریم واقعی بدون بافر کردن کل سطر (ستون‌ها باید به ترتیب خوانده شوند)
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            yield return new TaxInvoiceReportItem(
                Id: reader.GetInt64(0),
                // ستون ممکن است bigint یا nvarchar باشد؛ در هر دو حالت به string تبدیل می‌شود
                TaxSerialNumber: reader.IsDBNull(1)
                    ? null
                    : Convert.ToString(reader.GetValue(1), CultureInfo.InvariantCulture),
                InvoiceNumber: reader.GetString(2),
                InvoiceDate: reader.GetDateTime(3),
                CashPaymentAmount: reader.IsDBNull(4) ? null : reader.GetDecimal(4)
            );
        }
    }

    /// <summary>
    /// فقط شرط‌هایی که در criteria مقدار دارند به کوئری اضافه می‌شوند. همه مقادیر پارامتر هستند (ضد SQL Injection).
    /// </summary>
    private static string BuildQuery(InvoiceExportCriteria criteria, SqlParameterCollection parameters)
    {
        // بدون NOLOCK: برای داده مالی نباید dirty read داشته باشیم.
        // ترتیب بر اساس (InvoiceDate, Id) تا ردیف‌های هم‌تاریخ هم ترتیب قطعی داشته باشند.
        var sql = new StringBuilder("""
            SELECT
                Id,
                TaxSerialNumber,
                InvoiceNumber,
                InvoiceDate,
                CashPaymentAmount
            FROM [dbo].[FactInvoice]
            WHERE 1 = 1
            """);

        if (criteria.FromInvoiceDate is { } from)
        {
            sql.Append("\n  AND InvoiceDate >= @FromDate");
            parameters.Add(new SqlParameter("@FromDate", SqlDbType.DateTime2) { Value = from });
        }

        if (criteria.ToInvoiceDate is { } to)
        {
            sql.Append("\n  AND InvoiceDate <= @ToDate");
            parameters.Add(new SqlParameter("@ToDate", SqlDbType.DateTime2) { Value = to });
        }

        if (!string.IsNullOrWhiteSpace(criteria.TaxSerialNumber))
        {
            sql.Append("\n  AND TaxSerialNumber = @TaxSerialNumber");
            parameters.Add(new SqlParameter("@TaxSerialNumber", SqlDbType.NVarChar, 50) { Value = criteria.TaxSerialNumber });
        }

        if (!string.IsNullOrWhiteSpace(criteria.InvoiceNumber))
        {
            sql.Append("\n  AND InvoiceNumber = @InvoiceNumber");
            parameters.Add(new SqlParameter("@InvoiceNumber", SqlDbType.NVarChar, 100) { Value = criteria.InvoiceNumber });
        }

        sql.Append("\nORDER BY InvoiceDate ASC, Id ASC;");
        return sql.ToString();
    }
}
