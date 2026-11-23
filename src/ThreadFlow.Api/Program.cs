using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;
using ThreadFlow.Infrastructure.data.Master;
using ThreadFlow.Infrastructure.data.Tenant;
using ThreadFlow.Api.Modules.CustomerServiceAndLoyalty;
using System.Text.Json.Serialization;
using ThreadFlow.Api.Modules.Sales;
using ThreadFlow.Api.Modules.ManagementApprovalsReporting;
using ThreadFlow.Api.Tenancy;
using ThreadFlow.Infrastructure.Tenancy;
using ThreadFlow.Api.Authentication;
using ThreadFlow.Api.Onboarding;

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

builder.Services.AddIdentity<PlatformUser, IdentityRole<Guid>>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 10;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<MasterDbContext>()
    .AddDefaultTokenProviders();

var jwtSigningKey = builder.Configuration["Authentication:Jwt:SigningKey"];
if (string.IsNullOrWhiteSpace(jwtSigningKey))
{
    if (!builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException(
            "Authentication:Jwt:SigningKey must be configured outside Development.");
    }

    jwtSigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    Console.WriteLine(
        "WARNING: Using an ephemeral development JWT signing key. Configure " +
        "Authentication:Jwt:SigningKey to keep development tokens valid across restarts.");
}

var signingKeyBytes = Encoding.UTF8.GetBytes(jwtSigningKey);
if (signingKeyBytes.Length < 32)
{
    throw new InvalidOperationException(
        "Authentication:Jwt:SigningKey must be at least 32 UTF-8 bytes.");
}

var jwtIssuer = builder.Configuration["Authentication:Jwt:Issuer"] ?? "ThreadFlow";
var jwtAudience = builder.Configuration["Authentication:Jwt:Audience"] ?? "ThreadFlow.Clients";
var tokenLifetimeMinutes = builder.Configuration.GetValue("Authentication:Jwt:AccessTokenMinutes", 30);
if (tokenLifetimeMinutes is < 1 or > 1440)
{
    throw new InvalidOperationException(
        "Authentication:Jwt:AccessTokenMinutes must be between 1 and 1440.");
}

var jwtSigningKeyObject = new SymmetricSecurityKey(signingKeyBytes);

builder.Services.AddSingleton(new AccessTokenIssuer(
    jwtSigningKeyObject,
    jwtIssuer,
    jwtAudience,
    TimeSpan.FromMinutes(tokenLifetimeMinutes)));
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = jwtSigningKeyObject,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(60),
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });
builder.Services.AddAuthorization();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddScoped<ICurrentTenantContext, CurrentTenantContext>();
builder.Services.AddScoped<TenantBranchAssignmentService>();
builder.Services.AddScoped<TenantDatabaseConnectionStringProvider>();
builder.Services.AddDbContext<TenantDbContext>((serviceProvider, options) =>
{
    var connectionString = serviceProvider
        .GetRequiredService<TenantDatabaseConnectionStringProvider>()
        .GetConnectionString();

    options.UseSqlServer(connectionString);
});

builder.Services.AddScoped<CreateCustomerInteraction>();
builder.Services.AddScoped<GetCustomerInteractions>();
builder.Services.AddScoped<CreateSale>();
builder.Services.AddScoped<GetSalesOperationalReport>();
builder.Services.AddScoped<GetSalesCatalog>();
builder.Services.AddScoped<ITenantDatabaseProvisioner, TenantDatabaseProvisioner>();

var app = builder.Build();

await PlatformAdminBootstrapper.EnsureConfiguredAdminAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();
