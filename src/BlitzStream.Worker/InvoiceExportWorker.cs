using System.Diagnostics;
using BlitzStream.Domain;
using BlitzStream.Domain.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace BlitzStream.Worker;

public class InvoiceExportWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InvoiceExportWorker> _logger;

    public InvoiceExportWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<InvoiceExportWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Invoice Export Worker started at: {Time}", DateTimeOffset.Now);

        // ثبت مصرف حافظه در نقطه شروع
        var initialMemory = GC.GetTotalMemory(forceFullCollection: true);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // ساخت اسکوپ برای سرویس‌های Scoped
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<ITaxInvoiceRepository>();

            // بازه زمانی تستی (می‌تونی بر اساس داده‌های تستی خودت تغییر بدی)
            var criteria = new InvoiceExportCriteria
            {
                FromInvoiceDate = new DateTime(2023, 01, 01),
                ToInvoiceDate = new DateTime(2027, 01, 01)
            };

            ulong recordCount = 0;
            decimal totalCashPayment = 0m;

            _logger.LogInformation("Streaming invoices from database...");

            // مصرف استریم با await foreach و احترام به Cancellation
            await foreach (var invoice in repository.StreamInvoicesAsync(criteria, stoppingToken))
            {
                recordCount++;
                totalCashPayment += invoice.CashPaymentAmount ?? 0;

                // هر 10,000 رکورد یک لاگ پیشرفت می‌اندازیم
                if (recordCount % 10_000 == 0)
                {
                    _logger.LogInformation(
                        "Processed: {Count:N0} records | Current Batch Total: {Sum:C0}",
                        recordCount,
                        totalCashPayment);
                }
            }

            stopwatch.Stop();
            var finalMemory = GC.GetTotalMemory(forceFullCollection: false);
            var memoryDeltaMb = (finalMemory - initialMemory) / (1024.0 * 1024.0);

            _logger.LogInformation(
                "Export Finished! Total Records: {Count:N0} in {Elapsed:g}. Memory Diff: {Memory:F2} MB",
                recordCount,
                stopwatch.Elapsed,
                memoryDeltaMb);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Streaming was canceled gracefully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during invoice streaming.");
        }
    }
}
