using BlitzStream.Application;
using BlitzStream.Domain.Contracts;
using BlitzStream.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// رجیستر کردن سرویس‌ها در DI
builder.Services.AddSingleton<ITaxInvoiceRepository, TaxInvoiceRepository>();
builder.Services.AddSingleton<IInvoiceExportSink, CsvExportService>();

builder.Services.AddControllers();

// محدودیت تعداد خروجی‌های هم‌زمان (هر خروجی یک Connection و یک Query سنگین است)
var maxConcurrentExports = builder.Configuration.GetValue("Export:MaxConcurrentExports", 2);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddConcurrencyLimiter("export", limiter =>
    {
        limiter.PermitLimit = maxConcurrentExports;
        limiter.QueueLimit = 0;
    });
});

var app = builder.Build();

app.UseRateLimiter();
app.MapControllers();

app.Run();
