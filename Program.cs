using Microsoft.EntityFrameworkCore;
using PropertyInvoiceScanner.Core.Interfaces;
using PropertyInvoiceScanner.Infrastructure;
using PropertyInvoiceScanner.Infrastructure.Data;
using PropertyInvoiceScanner.Infrastructure.Email;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=factura.db"));
builder.Services.AddTransient<IEmailProvider, OutlookEmailProvider>();
builder.Services.AddHostedService<EmailProcessingWorker>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
