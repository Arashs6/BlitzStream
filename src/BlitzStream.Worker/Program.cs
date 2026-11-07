using BlitzStream.Application;
using BlitzStream.Domain;
using BlitzStream.Domain.Contracts;
using BlitzStream.Infrastructure;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// ۱. رجیستر کردن سرویس‌ها در DI
builder.Services.AddSingleton<ITaxInvoiceRepository, TaxInvoiceRepository>();
builder.Services.AddSingleton<IInvoiceExportSink, CsvExportService>();

var app = builder.Build();

// ۲. اندپوینت دریافت گزارش و فیلتر بر اساس تاریخ
app.MapGet("/api/invoices/export", async (
    [FromQuery] DateTime? fromDate,
    [FromQuery] DateTime? toDate,
    ITaxInvoiceRepository repository,
    IInvoiceExportSink exportSink,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    // فیلتر ورودی (اگر کاربر نفرستاد، پیش‌فرض معقول می‌گذاریم)
    var criteria = new InvoiceExportCriteria
    {
        FromInvoiceDate = fromDate ?? DateTime.UtcNow.AddMonths(-1),
        ToInvoiceDate = toDate ?? DateTime.UtcNow
    };

    var fileName = $"invoices_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

    // تنظیم هدرهای پاسخ برای دانلود فایل استریم‌شده
    httpContext.Response.ContentType = "text/csv; charset=utf-8";
    httpContext.Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");

    var stream = repository.StreamInvoicesAsync(criteria, cancellationToken);

    // نوشتن مستقیم به استریم خروجی شبکه (Network Stream) کلاینت
    await exportSink.ExportToStreamAsync(stream, httpContext.Response.Body, cancellationToken);
});

app.Run();