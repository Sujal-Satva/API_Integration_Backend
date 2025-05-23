using DataAccess.Services;
using Microsoft.EntityFrameworkCore;
using QuickBookService.Interfaces;
using QuickBookService.Services;
using task_14.Data;
using task_14.Middleware;
using task_14.Repository;
using task_14.Services;
using XeroService.Interfaces;
using XeroService.Services;
using DataAccess.Helper;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IChartOfAccountRepository, ChartOfAccountRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IBillRepository, BillRepository>();
builder.Services.AddScoped<IVendorRepository, VendorRepository>();
builder.Services.AddScoped<IConnectionRepository, ConnectionRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();


builder.Services.AddScoped<IQuickBooksApiService, QuickBooksApiService>();
builder.Services.AddScoped<IQuickBooksProductService, QuickBooksProductService>();
builder.Services.AddScoped<IQuickBooksCustomerService, QuickBooksCustomerService>();
builder.Services.AddScoped<IQuickBooksInvoiceServices, QuickBooksInvoiceService>();
builder.Services.AddScoped<IQuickBooksBillService, QuickBooksBillService>();
builder.Services.AddScoped<IQuickBooksVendorService, QuickBooksVendorService>();

builder.Services.AddScoped<IXeroProductService, XeroProductService>();
builder.Services.AddScoped<IXeroCustomerService, XeroCustomerService>();
builder.Services.AddScoped<IXeroApiService, XeroApiService>();
builder.Services.AddScoped<IXeroInvoiceService, XeroInvoiceService>();
builder.Services.AddScoped<IXeroBillService, XeroBillService>();


builder.Services.AddScoped<SyncingFunction>();
builder.Services.AddControllers();
builder.Services.AddHttpClient();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost5173", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowLocalhost5173");

app.UseRouting();

app.MapWhen(context => context.Request.Path.StartsWithSegments("/api"), appBuilder =>
{
    appBuilder.UseMiddleware<TokenRefreshMiddleware>();
    appBuilder.UseAuthorization();
    appBuilder.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
    });
});

app.UseAuthorization();
app.MapControllers();

app.Run();
