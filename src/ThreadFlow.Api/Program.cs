using Microsoft.EntityFrameworkCore;
using ThreadFlow.Infrastructure.data.Master;
using ThreadFlow.Infrastructure.data.Tenant;
using ThreadFlow.Api.Modules.CustomerServiceAndLoyalty;
using System.Text.Json.Serialization;
using ThreadFlow.Api.Modules.Sales;
using ThreadFlow.Api.Modules.ManagementApprovalsReporting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter()));
var masterConnectionString = builder.Configuration.GetConnectionString("MasterErp")
    ?? throw new InvalidOperationException("Connection string 'MasterErp' is not configured.");
builder.Services.AddDbContext<MasterDbContext>(options =>
    options.UseSqlServer(masterConnectionString));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var tenantConnectionString = builder.Configuration.GetConnectionString("TenantBusiness")
    ?? throw new InvalidOperationException(
        "Connection string 'TenantBusiness' is not configured.");

builder.Services.AddDbContext<TenantDbContext>(options =>
    options.UseSqlServer(tenantConnectionString)); 

builder.Services.AddScoped<CreateCustomerInteraction>();
builder.Services.AddScoped<GetCustomerInteractions>();
builder.Services.AddScoped<CreateSale>();
builder.Services.AddScoped<GetSalesOperationalReport>();
builder.Services.AddScoped<GetSalesCatalog>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
