using Microsoft.EntityFrameworkCore;
using ThreadFlow.Infrastructure.data.Master;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
var masterConnectionString = builder.Configuration.GetConnectionString("MasterErp")
    ?? throw new InvalidOperationException("Connection string 'MasterErp' is not configured.");
builder.Services.AddDbContext<MasterDbContext>(options =>
    options.UseSqlServer(masterConnectionString));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

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
