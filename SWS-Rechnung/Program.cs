using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWSRechnung.Data;
using SWSRechnung.Services;
using SWSRechnung.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Damit der Model Binder Dezimalzahlen mit Punkt (von <input type="number">)
// korrekt liest, unabhängig von der Server-Kultur (z. B. de-DE).
builder.Services.AddControllersWithViews(options =>
{
    options.ModelBinderProviders.Insert(0, new InvariantDecimalModelBinderProvider());
});

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
                ?? "Data Source=sws_rechnung.db"));

builder.Services.AddScoped<NummernService>();
builder.Services.AddScoped<EinstellungenService>();
builder.Services.AddScoped<PdfService>();
builder.Services.AddScoped<ZugferdService>();
builder.Services.AddScoped<DocxService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // If schema is broken/missing, wipe and recreate
    try { db.Kunden.Any(); }
    catch { db.Database.EnsureDeleted(); }

    db.Database.EnsureCreated();

    //db.Database.Migrate();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
