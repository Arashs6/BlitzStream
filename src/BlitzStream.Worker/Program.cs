using BlitzStream.Domain.Contracts;
using BlitzStream.Infrastructure;
using BlitzStream.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// اضافه کردن ریپازیتوری به DI Container
builder.Services.AddScoped<ITaxInvoiceRepository, TaxInvoiceRepository>();
builder.Configuration.AddUserSecrets<Program>();
// اضافه کردن BackgroundService پردازشگر
builder.Services.AddHostedService<InvoiceExportWorker>();

var host = builder.Build();
host.Run();