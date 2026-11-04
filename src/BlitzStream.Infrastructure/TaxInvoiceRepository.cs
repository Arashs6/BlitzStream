using BlitzStream.Domain;
using BlitzStream.Domain.Contracts;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Runtime.CompilerServices;


namespace BlitzStream.Infrastructure;

public class TaxInvoiceRepository : ITaxInvoiceRepository
{
    private readonly string _connectionString;

    public TaxInvoiceRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Warehouse")
                            ?? throw new InvalidOperationException("Connection string 'Warehouse' not found."); ;
    }

    public async IAsyncEnumerable<TaxInvoiceReportItem> StreamInvoicesAsync(InvoiceExportCriteria criteria, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT 
                               Id,
                               TaxSerialNumber,
                               InvoiceNumber,
                               InvoiceDate,
                               CashPaymentAmount
                           FROM [dbo].[FactInvoice] WITH (NOLOCK)
                           WHERE InvoiceDate >= @FromDate 
                             AND InvoiceDate <= @ToDate
                           ORDER BY InvoiceDate ASC;
                           """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.CommandType = CommandType.Text;

        // جلوگیری از تایم‌اوت در کوئری‌های سنگین مالیاتی
        command.CommandTimeout = 120;

        // استفاده از پارامترهای مشخص برای جلوگیری از SQL Injection و کامپایل بهینه Execution Plan
        command.Parameters.Add(new SqlParameter("@FromDate", SqlDbType.DateTime2) { Value = criteria.FromInvoiceDate });
        command.Parameters.Add(new SqlParameter("@ToDate", SqlDbType.DateTime2) { Value = criteria.ToInvoiceDate });

        // CommandBehavior.SequentialAccess جادوی معمارانه برای استریم واقعی است!
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);

        // خواندن ترتیبی و سبک بدون بافرینگ سنگین
        while (await reader.ReadAsync(cancellationToken))
        {
            yield return new TaxInvoiceReportItem(
                Id: reader.GetInt64(0),
                TaxSerialNumber: reader.IsDBNull(1) ? null : reader.GetString(1),
                InvoiceNumber: reader.GetString(2),
                InvoiceDate: reader.GetDateTime(3),
                CashPaymentAmount: reader.IsDBNull(4) ? null : reader.GetDecimal(4)
            );
        }
    }
}