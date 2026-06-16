using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Sayartii.Api.Data;
using Sayartii.Api.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Build Database Context using SQLite for Demo Readiness
var connStr = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=sayartii.db";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connStr));

// Add Identity with relaxed password rules
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false; // هذا السطر يحل المشكلة اللي ظهرت
    options.Password.RequiredLength = 6;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Add Controllers
builder.Services.AddControllers();

// Add JWT Authentication
var jwtSecret = builder.Configuration["JWT:Secret"] ?? "SuperSecretKeyForSayartiiAppWhichIsVeryLongAndSecureHere123!!";
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Warm up DB connection in background AFTER app starts (non-blocking)
_ = Task.Run(async () =>
{
    await Task.Delay(3000); // wait for server to be fully up
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    int retries = 10;
    while (retries > 0)
    {
        try
        {
            logger.LogInformation("Warming up DB connection...");
            // Simple ping query to pre-warm the connection pool
            await db.Database.ExecuteSqlRawAsync("SELECT 1");
            // Ensure tables exist
            var databaseCreator = db.Database.GetService<IRelationalDatabaseCreator>();
            try { databaseCreator.CreateTables(); } catch { /* already exist */ }
            logger.LogInformation("DB warm-up complete.");
            break;
        }
        catch (Exception ex)
        {
            retries--;
            logger.LogWarning($"DB warm-up failed, retrying... ({retries} left): {ex.Message}");
            await Task.Delay(5000);
        }
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Use Authentication before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
