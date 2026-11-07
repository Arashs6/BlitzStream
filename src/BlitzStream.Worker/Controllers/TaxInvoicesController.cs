using BlitzStream.Application;
using BlitzStream.Domain;
using BlitzStream.Domain.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BlitzStream.Worker.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TaxInvoicesController : ControllerBase
{
    private const int DefaultMaxRangeDays = 366;

    private readonly ITaxInvoiceRepository _repository;
    private readonly IInvoiceExportSink _exportSink;
    private readonly ILogger<TaxInvoicesController> _logger;
    private readonly int _maxRangeDays;

    public TaxInvoicesController(
        ITaxInvoiceRepository repository,
        IInvoiceExportSink exportSink,
        IConfiguration configuration,
        ILogger<TaxInvoicesController> logger)
    {
        _repository = repository;
        _exportSink = exportSink;
        _logger = logger;
        _maxRangeDays = configuration.GetValue("Export:MaxRangeDays", DefaultMaxRangeDays);
    }

    [HttpGet("export-csv")]
    [EnableRateLimiting("export")]
    public async Task<IActionResult> ExportCsv(
        [FromQuery] InvoiceExportCriteria criteria,
        CancellationToken cancellationToken)
    {
        // اگر بازه تاریخ ارسال نشده بود، پیش‌فرض (یک ماه اخیر) اعمال می‌شود
        criteria = criteria.WithDefaultDateRange();

        if (criteria.FromInvoiceDate > criteria.ToInvoiceDate)
            return BadRequest("FromInvoiceDate must be earlier than or equal to ToInvoiceDate.");

        if ((criteria.ToInvoiceDate - criteria.FromInvoiceDate)!.Value.TotalDays > _maxRangeDays)
            return BadRequest($"Date range must not exceed {_maxRangeDays} days.");

        Response.ContentType = "text/csv; charset=utf-8";

        var fileName = $"TaxInvoices_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");

        try
        {
            var items = _repository.StreamInvoicesAsync(criteria, cancellationToken);
            await _exportSink.ExportToStreamAsync(items, Response.Body, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // کلاینت اتصال را قطع کرده؛ خطا نیست
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CSV export failed.");

            // هدر 200 قبلاً ارسال شده؛ با قطع اتصال، کلاینت متوجه ناقص بودن فایل می‌شود
            // (نه اینکه یک فایل ناقص را کامل فرض کند).
            if (Response.HasStarted)
                HttpContext.Abort();
            else
                throw;
        }

        return new EmptyResult();
    }
}
