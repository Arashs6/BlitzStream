using System.Diagnostics;
using BlitzStream.Domain;
using BlitzStream.Domain.Contracts;

namespace BlitzStream.Worker;

/// <summary>
/// بنچمارک استریم فاکتورها از دیتابیس (در Program.cs ثبت نشده؛ برای اجرا باید AddHostedService شود).
/// بازه تاریخ از کانفیگ Benchmark:FromDate و Benchmark:ToDate خوانده می‌شود.
/// </summary>
public class InvoiceExportWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<InvoiceExportWorker> _logger;

    public InvoiceExportWorker(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<InvoiceExportWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Invoice Export Worker started at: {Time}", DateTimeOffset.Now);

        // میزان حافظه تخصیص‌یافته (allocated) قبل از شروع؛ GetTotalMemory عدد پایداری نمی‌دهد
        var initialAllocated = GC.GetTotalAllocatedBytes(precise: true);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<ITaxInvoiceRepository>();

            var criteria = new InvoiceExportCriteria
            {
                FromInvoiceDate = _configuration.GetValue("Benchmark:FromDate", new DateTime(2023, 01, 01)),
                ToInvoiceDate = _configuration.GetValue("Benchmark:ToDate", new DateTime(2027, 01, 01))
            };

            ulong recordCount = 0;
            decimal totalCashPayment = 0m;

            _logger.LogInformation("Streaming invoices from database...");

            await foreach (var invoice in repository.StreamInvoicesAsync(criteria, stoppingToken))
            {
                recordCount++;
                totalCashPayment += invoice.CashPaymentAmount ?? 0;

                if (recordCount % 10_000 == 0)
                {
                    _logger.LogInformation(
                        "Processed: {Count:N0} records | Running Total: {Sum:N0}",
                        recordCount,
                        totalCashPayment);
                }
            }

            stopwatch.Stop();
            var allocatedMb = (GC.GetTotalAllocatedBytes(precise: true) - initialAllocated) / (1024.0 * 1024.0);
            var peakWorkingSetMb = Process.GetCurrentProcess().PeakWorkingSet64 / (1024.0 * 1024.0);

            _logger.LogInformation(
                "Export Finished! Total Records: {Count:N0} in {Elapsed:g}. Allocated: {Allocated:F2} MB, Peak Working Set: {Peak:F2} MB",
                recordCount,
                stopwatch.Elapsed,
                allocatedMb,
                peakWorkingSetMb);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Streaming was canceled gracefully.");
        }
        // سایر خطاها عمداً گرفته نمی‌شوند: BackgroundService آن‌ها را log می‌کند و Host متوقف می‌شود،
        // تا شکست بنچمارک بی‌صدا نماند.
    }
}
