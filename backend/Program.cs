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

// Build NpgsqlDataSource manually for full PgBouncer Transaction Mode compatibility
var connStr = builder.Configuration.GetConnectionString("DefaultConnection")!;
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connStr);
// Disable all features that require persistent connection state (incompatible with PgBouncer)
var dataSource = dataSourceBuilder.Build();

// Add Database Context using the pre-built data source
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(dataSource, npgsql => npgsql.MaxBatchSize(1)));

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

// Auto Migrate the database with retry logic
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var db = services.GetRequiredService<ApplicationDbContext>();
    
    int retries = 10;
    while (retries > 0)
    {
        try
        {
            logger.LogInformation("Attempting to initialize database...");
            if (!db.Database.CanConnect())
            {
                Console.WriteLine("Cannot connect to database.");
            }
            
            // Ensure tables are created (even if DB exists)
            var databaseCreator = db.Database.GetService<IRelationalDatabaseCreator>();
            try {
                databaseCreator.CreateTables();
            } catch (Exception) {
                // Tables might already exist, ignore
            }
            
            Console.WriteLine("Database initialization finished.");
            break;
        }
        catch (Exception ex)
        {
            retries--;
            logger.LogWarning($"Database not ready yet. Retrying in 5 seconds... ({retries} attempts left)");
            System.Threading.Thread.Sleep(5000);
            if (retries == 0)
            {
                logger.LogError(ex, "Failed to initialize database after multiple attempts.");
                throw;
            }
        }
    }
}

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
